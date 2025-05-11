import torch
import torch.nn as nn
import os

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
        
        # Initialize with much larger random weights for more exploration
        for layer in self.actor:
            if isinstance(layer, nn.Linear):
                nn.init.xavier_uniform_(layer.weight, gain=5.0)
                nn.init.constant_(layer.bias, 0.0)
        
        for layer in self.critic:
            if isinstance(layer, nn.Linear):
                nn.init.xavier_uniform_(layer.weight, gain=5.0)
                nn.init.constant_(layer.bias, 0.0)
        
        # Initialize log_std with much larger values for more exploration
        self.log_std = nn.Parameter(torch.ones(output_dim) * 2.0)
        
    def forward(self, state):
        action_mean = self.actor(state)
        value = self.critic(state)
        return action_mean, value

def export_model(model_path, onnx_path):
    # Create model
    model = ActorCritic(6, 64, 3)
    
    # Load weights if they exist
    if os.path.exists(model_path):
        model.load_state_dict(torch.load(model_path))
    
    # Set to eval mode
    model.eval()
    
    # Create dummy input
    dummy_input = torch.randn(1, 6)
    
    # Export to ONNX with opset version 9 (compatible with Barracuda)
    torch.onnx.export(
        model,
        dummy_input,
        onnx_path,
        input_names=['input'],
        output_names=['action', 'value'],
        opset_version=9,  # Use opset version 9 for Barracuda compatibility
        dynamic_axes={
            'input': {0: 'batch_size'},
            'action': {0: 'batch_size'},
            'value': {0: 'batch_size'}
        }
    )
    print(f"Model exported to {onnx_path}")

if __name__ == "__main__":
    # Create runtime_models directory if it doesn't exist
    os.makedirs("runtime_models", exist_ok=True)
    
    # Export models for both players
    export_model("global_model_player1.pth", "runtime_models/actor_critic_player1.onnx")
    export_model("global_model_player2.pth", "runtime_models/actor_critic_player2.onnx") 