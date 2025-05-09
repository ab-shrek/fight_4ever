#!/bin/bash

echo "Starting Unity build process..."
echo "This might take a few minutes..."

UNITY_PATH="/Applications/Unity/Hub/Editor/6000.1.1f1/Unity.app/Contents/MacOS/Unity"

"$UNITY_PATH" -batchmode -nographics -projectPath "$(pwd)" -buildTarget StandaloneOSX -executeMethod Builder.BuildGame -quit -logFile build.log

if [ $? -eq 0 ]; then
    echo "Build completed successfully!"
    exit 0
else
    echo "Build failed! Check build.log for details"
    exit 1
fi 