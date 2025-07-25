#!/bin/bash

echo "=== Running Archistrateia NUnit Tests ==="
echo "Building project..."
dotnet build

if [ $? -eq 0 ]; then
    echo "Build successful. Running NUnit tests..."
    /Applications/Godot_mono.app/Contents/MacOS/Godot --headless --quit-after 15 --main-scene res://Scenes/NUnitTestScene.tscn
else
    echo "Build failed. Please fix compilation errors first."
    exit 1
fi 