import subprocess
import time
import os
import json
import shutil
from datetime import datetime
import signal
import sys
import requests
import logging

# Set up logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')

class GameInstanceManager:
    def __init__(self, num_instances=10, game_duration=120):
        self.num_instances = num_instances
        self.game_duration = game_duration
        self.model_dir = "trained_models"
        self.current_cycle = 0
        self.instances = []
        self.training_server_url = "http://localhost:5000"  # Add training server URL
        self.accuracy_stats = {
            'total_shots': 0,
            'total_hits': 0,
            'player1_shots': 0,
            'player1_hits': 0,
            'player2_shots': 0,
            'player2_hits': 0
        }
        
        # Create model directory if it doesn't exist
        os.makedirs(self.model_dir, exist_ok=True)
        
        # Set up accuracy log file in training/build/logs
        log_dir = os.path.join("training", "build", "logs")
        os.makedirs(log_dir, exist_ok=True)
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        self.accuracy_log_path = os.path.join(log_dir, f"accuracy_stats_{timestamp}.log")
        self.accuracy_logger = logging.getLogger('accuracy')
        self.accuracy_logger.setLevel(logging.INFO)
        
        # Create file handler for accuracy log
        accuracy_handler = logging.FileHandler(self.accuracy_log_path)
        accuracy_handler.setFormatter(logging.Formatter('%(asctime)s - %(message)s'))
        self.accuracy_logger.addHandler(accuracy_handler)
        
        logging.info(f"Accuracy statistics will be logged to: {self.accuracy_log_path}")
        
    def start_game_instances(self):
        logging.info(f"Starting {self.num_instances} game instances...")
        self.instances = []
        
        # Reset accuracy stats for new cycle
        self.accuracy_stats = {
            'total_shots': 0,
            'total_hits': 0,
            'player1_shots': 0,
            'player1_hits': 0,
            'player2_shots': 0,
            'player2_hits': 0
        }
        
        for i in range(self.num_instances):
            instance_id = f"game_{self.current_cycle}_{i}"
            logging.info(f"Starting game instance {instance_id}")
            
            # Set environment variables for the game instance
            env = os.environ.copy()
            env["INSTANCE_ID"] = instance_id
            env["TRAINING_SERVER_HOST"] = "localhost"  # Since server is on same machine
            
            # Start the game instance using the macOS app
            process = subprocess.Popen(
                #["open", "-a", "Fight4Ever.app", "--args", "--instance-id", instance_id],
                ["./Fight4Ever.x86_64"],
                env=env,
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE
            )
            self.instances.append(process)
            logging.info(f"Started game instance {instance_id} with PID {process.pid}")
            
        # Wait for all instances to complete
        logging.info(f"Waiting for {self.game_duration} seconds...")
        time.sleep(self.game_duration)
        
        # Kill any remaining instances
        for process in self.instances:
            try:
                process.terminate()
                process.wait(timeout=5)
            except:
                process.kill()
                
        # Print accuracy statistics
        self.print_accuracy_stats()
        
    def print_accuracy_stats(self):
        total_accuracy = (self.accuracy_stats['total_hits'] / self.accuracy_stats['total_shots'] * 100) if self.accuracy_stats['total_shots'] > 0 else 0
        player1_accuracy = (self.accuracy_stats['player1_hits'] / self.accuracy_stats['player1_shots'] * 100) if self.accuracy_stats['player1_shots'] > 0 else 0
        player2_accuracy = (self.accuracy_stats['player2_hits'] / self.accuracy_stats['player2_shots'] * 100) if self.accuracy_stats['player2_shots'] > 0 else 0
        
        # Log to console
        logging.info(f"\nCycle {self.current_cycle} Accuracy Statistics:")
        logging.info(f"Total Accuracy: {total_accuracy:.2f}% ({self.accuracy_stats['total_hits']}/{self.accuracy_stats['total_shots']} hits)")
        logging.info(f"Player 1 Accuracy: {player1_accuracy:.2f}% ({self.accuracy_stats['player1_hits']}/{self.accuracy_stats['player1_shots']} hits)")
        logging.info(f"Player 2 Accuracy: {player2_accuracy:.2f}% ({self.accuracy_stats['player2_hits']}/{self.accuracy_stats['player2_shots']} hits)")
        
        # Log to accuracy file
        self.accuracy_logger.info(f"Cycle {self.current_cycle} - Total: {total_accuracy:.2f}% ({self.accuracy_stats['total_hits']}/{self.accuracy_stats['total_shots']}) - P1: {player1_accuracy:.2f}% ({self.accuracy_stats['player1_hits']}/{self.accuracy_stats['player1_shots']}) - P2: {player2_accuracy:.2f}% ({self.accuracy_stats['player2_hits']}/{self.accuracy_stats['player2_shots']})")
        
    def update_accuracy_stats(self, player_id, hit):
        if player_id == 1:
            self.accuracy_stats['player1_shots'] += 1
            if hit:
                self.accuracy_stats['player1_hits'] += 1
                self.accuracy_stats['total_hits'] += 1
        else:
            self.accuracy_stats['player2_shots'] += 1
            if hit:
                self.accuracy_stats['player2_hits'] += 1
                self.accuracy_stats['total_hits'] += 1
        self.accuracy_stats['total_shots'] += 1
        
    def trigger_training(self):
        logging.info("Triggering training...")
        try:
            # Train player 1
            response1 = requests.post("http://localhost:5000/train")
            if response1.status_code == 200:
                result1 = response1.json()
                logging.info(f"Player 1 training results: Loss={result1.get('loss', 'N/A')}, "
                           f"Epsilon={result1.get('epsilon', 'N/A')}, "
                           f"Buffer size={result1.get('buffer_size', 'N/A')}")
            
            # Train player 2
            response2 = requests.post("http://localhost:5001/train")
            if response2.status_code == 200:
                result2 = response2.json()
                logging.info(f"Player 2 training results: Loss={result2.get('loss', 'N/A')}, "
                           f"Epsilon={result2.get('epsilon', 'N/A')}, "
                           f"Buffer size={result2.get('buffer_size', 'N/A')}")
                
        except Exception as e:
            logging.error(f"Error triggering training: {e}")
        
    def save_model_checkpoint(self):
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        cycle_dir = os.path.join(self.model_dir, f"cycle_{self.current_cycle}_{timestamp}")
        os.makedirs(cycle_dir, exist_ok=True)
        
        # Save model files
        model_files = [
            "player1_model.pt",
            "player2_model.pt",
            "player1_buffer.pt",
            "player2_buffer.pt",
            "training_metadata.json"
        ]
        
        for file in model_files:
            if os.path.exists(file):
                shutil.copy2(file, os.path.join(cycle_dir, file))
        
        # Save training metadata
        metadata = {
            "cycle": self.current_cycle,
            "timestamp": timestamp,
            "num_instances": self.num_instances,
            "game_duration": self.game_duration
        }
        
        with open(os.path.join(cycle_dir, "metadata.json"), "w") as f:
            json.dump(metadata, f, indent=2)
            
        logging.info(f"Saved model checkpoint to {cycle_dir}")
        
    def run_training_cycle(self):
        """Run a complete training cycle"""
        logging.info("Starting new training cycle")
        
        # Reset accuracy stats for this cycle
        self.accuracy_stats = {
            'total_shots': 0,
            'total_hits': 0,
            'player1_shots': 0,
            'player1_hits': 0,
            'player2_shots': 0,
            'player2_hits': 0
        }
        
        # Start game instances
        self.start_game_instances()
        
        # Wait for all instances to complete
        self.wait_for_instances()
        
        # Print accuracy stats for this cycle
        self.print_accuracy_stats()
        
        # Save model after cycle completion
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        model_dir = os.path.join("training", "build", "models", f"cycle_{self.current_cycle}_{timestamp}")
        os.makedirs(model_dir, exist_ok=True)
        
        # Save model weights with cycle number in filename
        model_path = os.path.join(model_dir, f"model_cycle_{self.current_cycle}.pt")
        try:
            response = requests.post(f"{self.training_server_url}/save_model", 
                                  json={"path": model_path})
            if response.status_code == 200:
                logging.info(f"Model saved to {model_path}")
            else:
                logging.error(f"Failed to save model: {response.text}")
        except Exception as e:
            logging.error(f"Error saving model: {str(e)}")
        
        # Save metadata
        metadata = {
            "timestamp": timestamp,
            "cycle_number": self.current_cycle,
            "num_instances": self.num_instances,
            "game_duration": self.game_duration,
            "accuracy_stats": self.accuracy_stats
        }
        metadata_path = os.path.join(model_dir, "metadata.json")
        with open(metadata_path, 'w') as f:
            json.dump(metadata, f, indent=2)
        
        self.current_cycle += 1
        logging.info(f"Training cycle {self.current_cycle} completed")
        
    def run(self, num_cycles=100):
        logging.info(f"Starting training for {num_cycles} cycles")
        logging.info(f"Each cycle will run {self.num_instances} instances for {self.game_duration} seconds")
        
        try:
            for cycle in range(num_cycles):
                logging.info(f"\n=== Starting Cycle {cycle + 1}/{num_cycles} ===")
                self.run_training_cycle()
                
        except KeyboardInterrupt:
            logging.info("\nTraining interrupted by user")
        finally:
            # Cleanup any remaining instances
            for process in self.instances:
                try:
                    process.terminate()
                except:
                    pass

    def wait_for_instances(self):
        """Wait for all game instances to complete"""
        logging.info("Waiting for all game instances to complete...")
        
        # Wait for the specified game duration
        time.sleep(self.game_duration)
        
        # Terminate any remaining instances
        for process in self.instances:
            try:
                process.terminate()
                process.wait(timeout=5)
            except:
                process.kill()
        
        logging.info("All game instances completed")

if __name__ == "__main__":
    # Parse command line arguments
    import argparse
    parser = argparse.ArgumentParser(description="Run multiple game instances for training")
    parser.add_argument("--instances", type=int, default=10, help="Number of game instances per cycle")
    parser.add_argument("--duration", type=int, default=120, help="Duration of each game in seconds")
    parser.add_argument("--cycles", type=int, default=100, help="Number of training cycles")
    args = parser.parse_args()
    
    # Create and run manager
    manager = GameInstanceManager(
        num_instances=args.instances,
        game_duration=args.duration
    )
    manager.run(num_cycles=args.cycles) 
