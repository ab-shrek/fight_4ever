import socket
import json
import numpy as np
import torch
import torch.nn as nn
import torch.optim as optim
from torch.distributions import Normal
import asyncio
import time
import uuid
import queue

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
    def __init__(self, host='0.0.0.0', port1=5000, port2=5001):
        self.host = host
        self.ports = [port1, port2]
        self.trainers = [PPOTrainer(6, 64, 3), PPOTrainer(6, 64, 3)]
        self.experience_queues = [queue.Queue(), queue.Queue()]
        print(f"Training server initialized on ports {port1} (Player 1) and {port2} (Player 2)")
    async def handle_client(self, reader, writer, player_id):
        client_id = str(uuid.uuid4())
        print(f"New client connected: {client_id} as Player {player_id}")
        buffer = ""
        while True:
            try:
                data = await reader.read(4096)
                if not data:
                    break
                buffer += data.decode('utf-8')
                while '\n' in buffer:
                    line, buffer = buffer.split('\n', 1)
                    observation = json.loads(line)
                    # Compose state
                    state = np.array([
                        observation['health'],
                        observation['position'][0],
                        observation['position'][1],
                        observation['opponent_health'],
                        observation['opponent_position'][0],
                        observation['opponent_position'][1]
                    ])
                    # Get action from PPO for this player
                    action = self.trainers[player_id-1].get_action(state)
                    response = {
                        'movement': action[:2].tolist(),
                        'attack': bool(action[2] > 0)
                    }
                    # Optionally, queue experience for training
                    self.experience_queues[player_id-1].put((client_id, state, action))
                    writer.write((json.dumps(response) + '\n').encode('utf-8'))
                    await writer.drain()
            except Exception as e:
                print(f"Client {client_id} (Player {player_id}) error: {e}")
                break
        print(f"Client disconnected: {client_id} (Player {player_id})")
        writer.close()
        await writer.wait_closed()
    async def start(self):
        servers = []
        for idx, port in enumerate(self.ports):
            # Pass player_id (1 or 2) to handler
            server = await asyncio.start_server(
                lambda r, w, pid=idx+1: self.handle_client(r, w, pid),
                self.host, port)
            servers.append(server)
            print(f"Server started on {self.host}:{port} for Player {idx+1}")
            # Start training loop for each player/model
            asyncio.create_task(self.training_loop(idx))
        # Serve all servers concurrently
        await asyncio.gather(*(s.serve_forever() for s in servers))
    async def training_loop(self, trainer_idx):
        print(f"Training loop started for Player {trainer_idx+1} (dummy, add batching and updates as needed)")
        while True:
            try:
                if not self.experience_queues[trainer_idx].empty():
                    client_id, state, action = self.experience_queues[trainer_idx].get()
                    # Here you would batch experiences and update the model
                    # For now, just print
                    print(f"Training on experience from {client_id} (Player {trainer_idx+1})")
            except Exception as e:
                print(f"Training loop error for Player {trainer_idx+1}: {e}")
            await asyncio.sleep(0.01)

if __name__ == "__main__":
    server = TrainingServer()
    asyncio.run(server.start()) 