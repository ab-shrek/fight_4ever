import socket
import json
import numpy as np
import torch
import torch.nn as nn
import torch.optim as optim
from torch.distributions import Normal
import threading
import queue
import time

class ActorCritic(nn.Module):
    def __init__(self, input_dim, hidden_dim, output_dim):
        super(ActorCritic, self).__init__()
        self.actor = nn.Sequential(
            nn.Linear(input_dim, hidden_dim),
            nn.ReLU(),
            nn.Linear(hidden_dim, hidden_dim),
            nn.ReLU(),
            nn.Linear(hidden_dim, output_dim),
            nn.Tanh()
        )
        
        self.critic = nn.Sequential(
            nn.Linear(input_dim, hidden_dim),
            nn.ReLU(),
            nn.Linear(hidden_dim, hidden_dim),
            nn.ReLU(),
            nn.Linear(hidden_dim, 1)
        )
        
        self.log_std = nn.Parameter(torch.zeros(output_dim))
        
    def forward(self, state):
        action_mean = self.actor(state)
        value = self.critic(state)
        return action_mean, value

class PPOTrainer:
    def __init__(self, input_dim, hidden_dim, output_dim, device='cuda'):
        self.device = torch.device(device if torch.cuda.is_available() else 'cpu')
        print(f"Using device: {self.device}")
        if self.device.type == 'cpu':
            print("Running on CPU. Training will be slower but still functional.")
        self.model = ActorCritic(input_dim, hidden_dim, output_dim).to(self.device)
        self.optimizer = optim.Adam(self.model.parameters(), lr=3e-4)
        self.gamma = 0.99
        self.gae_lambda = 0.95
        self.clip_ratio = 0.2
        self.value_coef = 0.5
        self.entropy_coef = 0.01
        
    def get_action(self, state):
        with torch.no_grad():
            state = torch.FloatTensor(state).to(self.device)
            action_mean, _ = self.model(state)
            std = torch.exp(self.model.log_std)
            dist = Normal(action_mean, std)
            action = dist.sample()
            return action.cpu().numpy()

class TrainingServer:
    def __init__(self, host='0.0.0.0', base_port=5000):
        self.host = host
        self.base_port = base_port
        
        # Initialize two servers, one for each agent
        self.servers = []
        self.trainers = []
        self.observation_queues = []
        self.action_queues = []
        
        # Create two separate servers and trainers
        for i in range(2):
            port = base_port + i
            server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            server_socket.bind((self.host, port))
            server_socket.listen(5)
            self.servers.append(server_socket)
            
            # Initialize PPO trainer for this agent
            input_dim = 6  # health, pos_x, pos_z, opp_health, opp_pos_x, opp_pos_z
            hidden_dim = 64
            output_dim = 3  # move_x, move_z, shoot
            trainer = PPOTrainer(input_dim, hidden_dim, output_dim)
            self.trainers.append(trainer)
            
            # Create queues for this agent
            self.observation_queues.append(queue.Queue())
            self.action_queues.append(queue.Queue())
            
            # Start training thread for this agent
            training_thread = threading.Thread(target=self._training_loop, args=(i,))
            training_thread.daemon = True
            training_thread.start()
            
            print(f"Initialized server for agent {i+1} on port {port}")
        
        print("Training servers initialized successfully")
        
    def _training_loop(self, agent_id):
        print(f"Training loop started for agent {agent_id+1}")
        while True:
            try:
                if not self.observation_queues[agent_id].empty():
                    observation = self.observation_queues[agent_id].get()
                    print(f"Agent {agent_id+1} received observation: {observation}")
                    
                    # Convert observation to state tensor
                    state = np.array([
                        observation['health'],
                        observation['position'][0],
                        observation['position'][1],
                        observation['opponent_health'],
                        observation['opponent_position'][0],
                        observation['opponent_position'][1]
                    ])
                    
                    # Get action from PPO
                    action = self.trainers[agent_id].get_action(state)
                    print(f"Agent {agent_id+1} generated action: {action}")
                    
                    # Convert action to response format
                    response = {
                        'movement': action[:2].tolist(),
                        'attack': bool(action[2] > 0)
                    }
                    
                    # Store in action queue for client response
                    self.action_queues[agent_id].put(response)
            except Exception as e:
                print(f"Training error for agent {agent_id+1}: {e}")
            time.sleep(0.001)  # Prevent CPU overload
    
    def handle_client(self, client_socket, agent_id):
        print(f"New client connected for agent {agent_id+1} from {client_socket.getpeername()}")
        buffer = ""
        while True:
            try:
                data = client_socket.recv(4096).decode('utf-8')
                if not data:
                    break
                    
                buffer += data
                while '\n' in buffer:
                    line, buffer = buffer.split('\n', 1)
                    observation = json.loads(line)
                    print(f"Agent {agent_id+1} received observation: {observation}")
                    
                    # Add observation to training queue
                    self.observation_queues[agent_id].put(observation)
                    
                    # Wait for action response
                    try:
                        response = self.action_queues[agent_id].get(timeout=0.5)
                        print(f"Agent {agent_id+1} sending response: {response}")
                        client_socket.sendall((json.dumps(response) + '\n').encode('utf-8'))
                    except queue.Empty:
                        print(f"Timeout waiting for action for agent {agent_id+1}, sending default action")
                        default_response = {
                            'movement': [0.1, 0.1],  # Slight movement
                            'attack': False
                        }
                        client_socket.sendall((json.dumps(default_response) + '\n').encode('utf-8'))
                            
            except Exception as e:
                print(f"Client handling error for agent {agent_id+1}: {e}")
                break
                
        print(f"Client disconnected for agent {agent_id+1}: {client_socket.getpeername()}")
        client_socket.close()
    
    def start(self):
        print(f"Servers started on ports {self.base_port} and {self.base_port+1}")
        print("Waiting for Unity client connections...")
        
        # Start server threads for both agents
        for i in range(2):
            server_thread = threading.Thread(target=self._server_loop, args=(i,))
            server_thread.daemon = True
            server_thread.start()
        
        # Keep main thread alive
        while True:
            time.sleep(1)
    
    def _server_loop(self, agent_id):
        while True:
            try:
                client_socket, address = self.servers[agent_id].accept()
                client_thread = threading.Thread(
                    target=self.handle_client,
                    args=(client_socket, agent_id)
                )
                client_thread.daemon = True
                client_thread.start()
            except Exception as e:
                print(f"Server error for agent {agent_id+1}: {e}")

if __name__ == "__main__":
    server = TrainingServer()
    server.start() 