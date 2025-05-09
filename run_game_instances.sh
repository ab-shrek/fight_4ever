#!/bin/bash

# Usage: ./run_game_instances.sh [NUM_INSTANCES]
NUM_INSTANCES=${1:-3}
GAME_PATH="/Users/mario/fight_4ever/Build/Fight4Ever.app/Contents/MacOS/fight_4ever"
LOG_DIR="logs"
MAP_TYPES=(default)

mkdir -p "$LOG_DIR"

echo "Launching $NUM_INSTANCES game instances..."

for i in $(seq 1 $NUM_INSTANCES); do
  # Pick a random MAP_TYPE
  MAP_TYPE=${MAP_TYPES[$((RANDOM % ${#MAP_TYPES[@]}))]}
  LOG_FILE="$LOG_DIR/client_$i.log"
  echo "Starting instance $i with MAP_TYPE=$MAP_TYPE, logging to $LOG_FILE"
  INSTANCE_ID=$i MAP_TYPE=$MAP_TYPE "$GAME_PATH" > "$LOG_FILE" 2>&1 &
done

wait
echo "All $NUM_INSTANCES game instances have finished." 