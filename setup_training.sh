#!/bin/bash

# Get the parent directory of the current project
PARENT_DIR="$(cd .. && pwd)"
TRAINING_DIR="$PARENT_DIR/fight_4ever_training"

echo "Setting up training environment in: $TRAINING_DIR"

# Create necessary directories
mkdir -p "$TRAINING_DIR"
mkdir -p "$TRAINING_DIR/build"
mkdir -p "$TRAINING_DIR/models"

# Copy Python training script
cp training/gpu_training_server.py "$TRAINING_DIR/"

# Create a requirements.txt file
echo "torch
numpy
tensorboard" > "$TRAINING_DIR/requirements.txt"

# Create a run script
cat > "$TRAINING_DIR/run_training.sh" << 'EOF'
#!/bin/bash

# Check if Python environment exists, if not create it
if [ ! -d "venv" ]; then
    echo "Creating Python virtual environment..."
    python3 -m venv venv
    source venv/bin/activate
    pip install -r requirements.txt
else
    source venv/bin/activate
fi

# Start the training server
python gpu_training_server.py
EOF

# Make run script executable
chmod +x "$TRAINING_DIR/run_training.sh"

# Create a README
cat > "$TRAINING_DIR/README.md" << 'EOF'
# Fight4Ever Training Environment

This directory contains the training environment for the Fight4Ever game.

## Directory Structure
- `build/`: Place your Unity build here
- `models/`: Trained models will be saved here
- `gpu_training_server.py`: The training server implementation
- `requirements.txt`: Python dependencies
- `run_training.sh`: Script to start the training server

## Setup
1. Build your Unity project and copy the build to `build/`
2. Run `./run_training.sh` to start the training server
3. Run the Unity build from the `build/` directory

## Notes
- The training server will automatically use GPU if available, otherwise CPU
- Models are saved in the `models/` directory
EOF

echo "Setup complete! Training environment created in: $TRAINING_DIR"
echo ""
echo "Next steps:"
echo "1. Build your Unity project and copy the build to $TRAINING_DIR/build/"
echo "2. cd $TRAINING_DIR"
echo "3. ./run_training.sh" 