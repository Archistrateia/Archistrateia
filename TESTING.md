# Archistrateia Testing & Diagnostics Guide

## Overview

This document describes the comprehensive testing and diagnostic system implemented for the Archistrateia project. The system includes automated tests, real-time diagnostics, and detailed logging to help identify and resolve issues.

## Automated Testing

### Running Tests

#### Quick Test Run
```bash
./run_tests.sh
```

This script will:
1. Build the C# project
2. Run all NUnit tests using the NUnit test runner
3. Display test results

#### Manual Test Run
```bash
# Build the project
dotnet build

# Run NUnit tests
/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --quit-after 15 --main-scene res://Scenes/NUnitTestScene.tscn
```

### Test Coverage

The NUnit test suite covers:

1. **HexGridPositionTest** - Unit tests for hex position calculations
2. **HexGridIntegrationTest** - Integration tests for HexGridCalculator
3. **IntegrationTests** - Comprehensive game system tests:
   - Terrain color initialization
   - Map generation (10x10 grid)
   - Hex tile creation and structure
   - Signal connections
   - HexGridCalculator integration
   - Game enums validation
   - HexGridCalculator constants validation

### Test Results

All tests should pass with 100% success rate:
```
=== NUnit Test Results ===
Total Tests: 15
Passed: 15
Failed: 0
Success Rate: 100.0%
🎉 ALL NUnit TESTS PASSED! 🎉
```

## Diagnostic System

### Real-Time Diagnostics

The `DiagnosticMonitor` class provides real-time feedback about:

- **Scene Information**: Current scene, node count
- **Main Instance Status**: Whether Main class is found and accessible
- **Map Container Status**: Whether map has been generated and tile count
- **UI Element Status**: Start button and title label availability
- **Performance Metrics**: FPS, memory usage
- **Input Information**: Mouse position, window size

### Using Diagnostics

#### In-Game Controls
- **F1**: Toggle diagnostic display on/off
- **F2**: Force manual diagnostic update

#### Adding Diagnostics to Main Scene

To add the diagnostic monitor to your main scene:

1. Add a `DiagnosticMonitor` node to your scene
2. The monitor will automatically start collecting data
3. Press F1 to toggle the diagnostic overlay

### Diagnostic Output Example

```
=== DIAGNOSTIC INFO ===
Time: 10:37:45
FPS: 60
Memory: 45 MB

=== SCENE INFO ===
Scene: Main
Nodes: 15

=== MAIN INSTANCE ===
Status: Found
Path: /root/Main
Map Container: Found
Map Tiles: 100
Start Button: Found
Button Visible: False
Title Label: Found

=== INPUT INFO ===
Mouse: (512, 384)
Window: (1024, 768)
```

## Detailed Logging

### Main.cs Diagnostic Logging

The `Main.cs` file includes comprehensive logging with clear section markers:

```
=== MAIN: _Ready() called ===
Main loaded. Node path: /root/Main
StartButton reference: VALID
TitleLabel reference: VALID
TurnManager reference: NULL
=== MAIN: _Ready() completed ===

=== MAIN: OnStartButtonPressed() called ===
Start Button Pressed. Game Starting...
Hiding StartButton
Calling GenerateMap()
=== MAIN: GenerateMap() called ===
Creating new map container
MapContainer added to scene at path: /root/Main/MapContainer
Created 25 tiles...
Created 50 tiles...
Created 75 tiles...
Created 100 tiles...
Generated hex map with 100 tiles
MapContainer now has 100 children
=== MAIN: GenerateMap() completed ===
```

### Log Levels

- **INFO**: Normal operation messages
- **ERROR**: Critical issues that need attention
- **WARNING**: Potential issues or unexpected states

## Troubleshooting

### Common Issues

#### Signal Connection Problems
- Check that the signal is properly connected in the scene file
- Verify the method name matches exactly (case-sensitive)
- Ensure the target node exists and is accessible

#### Map Not Generating
- Check console for error messages
- Verify `OnStartButtonPressed()` is being called
- Ensure `GenerateMap()` method is executing
- Check that `MapContainer` is being created

#### Performance Issues
- Monitor FPS in diagnostic display
- Check memory usage for leaks
- Verify tile count matches expected (100 tiles)

### Debugging Steps

1. **Run Automated Tests**: `./run_tests.sh`
2. **Check Console Output**: Look for error messages and diagnostic info
3. **Enable Diagnostic Monitor**: Add to scene and press F1
4. **Verify Signal Connections**: Check scene file and inspector
5. **Test Individual Components**: Use test scene to isolate issues

## File Structure

```
Scripts/
├── Main.cs                 # Main game logic
├── HexGridCalculator.cs    # Hex grid calculation utilities
└── [other game scripts]

Scenes/
├── Main.tscn              # Main game scene
├── NUnitTestScene.tscn    # NUnit test scene
└── [other scenes]

Tests/
├── TestRunner.cs              # NUnit test runner for Godot
├── HexGridPositionTest.cs     # Unit tests for hex positioning
├── HexGridIntegrationTest.cs  # Integration tests for hex grid
├── IntegrationTests.cs        # Comprehensive game system tests
└── [test scene files]

run_tests.sh               # NUnit test execution script
TESTING.md                 # This documentation
```

## Best Practices

1. **Always run tests** before committing changes
2. **Check diagnostic output** when issues occur
3. **Use descriptive log messages** with clear section markers
4. **Test individual components** before integration
5. **Monitor performance** during development

## Future Enhancements

- Unit tests for individual classes
- Performance benchmarking
- Visual test results
- Continuous integration setup
- Coverage reporting 