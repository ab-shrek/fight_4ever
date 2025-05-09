#!/bin/bash

# Get the parent directory of the current project
PARENT_DIR=$(dirname "$(pwd)")
TRAINING_DIR="$PARENT_DIR/fight_4ever_training"
BUILD_DIR="$TRAINING_DIR/build"

# Check if build directory exists
if [ ! -d "$BUILD_DIR" ]; then
    echo "Creating build directory..."
    mkdir -p "$BUILD_DIR"
fi

# Check if Unity build exists
if [ ! -d "build" ]; then
    echo "Error: Unity build directory not found!"
    echo "Please build your Unity project first."
    exit 1
fi

# Copy the build
echo "Copying Unity build to training directory..."
cp -R build/* "$BUILD_DIR/"

echo "Build copied successfully!"
echo "You can now start the training server by running:"
echo "cd $TRAINING_DIR && ./run_training.sh" 