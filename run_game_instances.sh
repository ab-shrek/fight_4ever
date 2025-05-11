#!/bin/bash

# Usage: ./run_game_instances.sh [NUM_INSTANCES] [NUM_ROUNDS]
NUM_INSTANCES=${1:-3}
NUM_ROUNDS=${2:-1}  # Default to 1 round if not specified
GAME_PATH="$(pwd)/Fight4Ever.x86_64"
LOG_DIR="logs"
MAP_TYPES=(default)
PY_TRAIN_SCRIPT="local_train_and_sync.py"  # Path to your local Python training script
ONNX_MODEL_SOURCE="actor_critic.onnx"  # Path to your latest ONNX model (update if needed)
ONNX_MODEL_DEST="Assets/Resources/actor_critic.onnx"
RUNTIME_MODEL_DIR="runtime_models"
RUNTIME_MODEL_PATH_PLAYER1="$RUNTIME_MODEL_DIR/actor_critic_player1.onnx"
RUNTIME_MODEL_PATH_PLAYER2="$RUNTIME_MODEL_DIR/actor_critic_player2.onnx"

mkdir -p "$LOG_DIR"

echo "Will run $NUM_ROUNDS rounds of $NUM_INSTANCES game instances each"

for round in $(seq 1 $NUM_ROUNDS); do
    echo "Starting round $round of $NUM_ROUNDS..."

    # Download the latest ONNX models for both players from the server before each round
    echo "Downloading latest ONNX models for Player 1 and Player 2 from server..."
    curl -o "$RUNTIME_MODEL_PATH_PLAYER1" http://localhost:5000/download_model_player1
    curl -o "$RUNTIME_MODEL_PATH_PLAYER2" http://localhost:5000/download_model_player2
    if [ -f "$RUNTIME_MODEL_PATH_PLAYER1" ]; then
        echo "Downloaded $RUNTIME_MODEL_PATH_PLAYER1 for round $round."
    else
        echo "WARNING: $RUNTIME_MODEL_PATH_PLAYER1 not found."
    fi
    if [ -f "$RUNTIME_MODEL_PATH_PLAYER2" ]; then
        echo "Downloaded $RUNTIME_MODEL_PATH_PLAYER2 for round $round."
    else
        echo "WARNING: $RUNTIME_MODEL_PATH_PLAYER2 not found."
    fi

    # Convert ONNX models to NNModel assets using Unity batchmode
    /Applications/Unity/Hub/Editor/6000.1.1f1/Unity.app/Contents/MacOS/Unity \
      -projectPath "$(pwd)" \
      -batchmode -quit \
      -executeMethod ONNXToNNModelConverter.ConvertAllONNXFiles

    # 1. Upload all experience files to the server in parallel (handled in local_train_and_sync.py)
    for i in $(seq 1 $NUM_INSTANCES); do
        for PLAYER_ID in 1 2; do
            PY_LOG_FILE="$LOG_DIR/round${round}_python_train_player${PLAYER_ID}_client_${i}.log"
            CLIENT_ID=$i
            echo "Uploading experience for Player $PLAYER_ID, instance $i, logging to $PY_LOG_FILE"
            cd "$(pwd)" && PLAYER_ID=$PLAYER_ID CLIENT_ID=$CLIENT_ID python3 "$PY_TRAIN_SCRIPT" > "$PY_LOG_FILE" 2>&1 &
        done
    done

    wait  # Wait for all experience uploads to finish

    # 2. Start all Unity game instances in parallel
    for i in $(seq 1 $NUM_INSTANCES); do
        for PLAYER_ID in 1 2; do
            MAP_TYPE=${MAP_TYPES[$((RANDOM % ${#MAP_TYPES[@]}))]}
            LOG_FILE="$LOG_DIR/round${round}_player${PLAYER_ID}_client_${i}.log"
            if [ "$PLAYER_ID" -eq 1 ]; then
                RUNTIME_MODEL_PATH="$RUNTIME_MODEL_PATH_PLAYER1"
            else
                RUNTIME_MODEL_PATH="$RUNTIME_MODEL_PATH_PLAYER2"
            fi
            echo "Starting Unity instance $i for Player $PLAYER_ID with MAP_TYPE=$MAP_TYPE, logging to $LOG_FILE, using model $RUNTIME_MODEL_PATH"
            PLAYER_ID=$PLAYER_ID INSTANCE_ID=$i MAP_TYPE=$MAP_TYPE RUNTIME_MODEL_PATH="$RUNTIME_MODEL_PATH" "$GAME_PATH" > "$LOG_FILE" 2>&1 &
        done
    done

    wait  # Wait for all Unity instances to finish
    echo "Round $round completed. All game instances and experience uploads have finished."
    
    # Optional: Add a small delay between rounds
    if [ $round -lt $NUM_ROUNDS ]; then
        echo "Waiting 5 seconds before starting next round..."
        sleep 5
    fi

done

echo "All $NUM_ROUNDS rounds completed successfully." 