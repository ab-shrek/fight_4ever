#!/bin/bash

set -e

echo "Starting Unity build process for Ubuntu..."
echo "This might take a few minutes..."

UNITY_PATH="/Applications/Unity/Hub/Editor/6000.1.1f1/Unity.app/Contents/MacOS/Unity"
PROJECT_PATH="$(pwd)"

# Minimal headless mode arguments
HEADLESS_ARGS="-batchmode -nographics -force-glcore -force-low-power-device"

# macOS build (commented out for now)
# echo "\n--- Building for macOS (StandaloneOSX) ---"
# "$UNITY_PATH" $HEADLESS_ARGS -force-gfx-metal -force-clamped \
#     -projectPath "$PROJECT_PATH" \
#     -buildTarget StandaloneOSX \
#     -executeMethod Builder.BuildMac \
#     -quit
# 
# if [ $? -eq 0 ]; then
#     echo "macOS build completed successfully!"
# else
#     echo "macOS build failed!"
#     exit 1
# fi

# Ubuntu/Linux build
echo "\n--- Building for Ubuntu/Linux (StandaloneLinux64) ---"
"$UNITY_PATH" $HEADLESS_ARGS \
    -projectPath "$PROJECT_PATH" \
    -buildTarget StandaloneLinux64 \
    -executeMethod Builder.BuildLinux \
    -quit

if [ $? -eq 0 ]; then
    echo "Linux build completed successfully!"
else
    echo "Linux build failed!"
    exit 1
fi

echo "\nAll builds completed successfully!"
exit 0 