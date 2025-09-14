# Archistrateia Testing & Diagnostics Guide

## Overview

This document describes the comprehensive testing and diagnostic system implemented for the Archistrateia project. The system includes automated tests, real-time diagnostics, and detailed logging to help identify and resolve issues.

## Comprehensive Testing Suite

### Running Tests

#### Quick Test Run (All Tests)
```bash
./run_tests.sh
```

This script will run all test phases:
1. **Phase 1**: NUnit Unit Tests (logic and calculations)
2. **Phase 2**: Godot Scene Tests (actual scene functionality)
3. **Phase 3**: UI Integration Tests (UI components and interactions)

#### Individual Test Phases
```bash
# Run only NUnit unit tests
./run_tests.sh nunit

# Run only Godot scene tests
./run_tests.sh scenes

# Run only UI integration tests
./run_tests.sh ui
```

#### AI-Optimized Output Mode
```bash
# Run all tests with AI-optimized output format
./run_tests.sh --ai-output

# Run specific phase with AI format
./run_tests.sh scenes --ai-output
./run_tests.sh nunit --ai-output
./run_tests.sh ui --ai-output
```

The `--ai-output` flag produces structured, machine-readable results perfect for AI analysis and automated systems. See the [AI Output Format](#ai-output-format) section below for details.

#### Manual Test Run
```bash
# Build the project
dotnet build

# Run individual test phases manually
/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --quit-after 20 --main-scene res://Scenes/NUnitTestScene.tscn
/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --quit-after 25 --main-scene res://Scenes/GodotSceneTestScene.tscn
/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --quit-after 20 --main-scene res://Scenes/UITestScene.tscn
```

### Test Coverage

#### Phase 1: NUnit Unit Tests
- **HexGridPositionTest** - Unit tests for hex position calculations
- **HexGridIntegrationTest** - Integration tests for HexGridCalculator
- **IntegrationTests** - Comprehensive game system tests:
  - Terrain color initialization
  - Map generation (10x10 grid)
  - Hex tile creation and structure
  - Signal connections
  - HexGridCalculator integration
  - Game enums validation
  - HexGridCalculator constants validation

#### Phase 2: Godot Scene Tests
- **Scene Loading** - Tests actual Main scene loading and initialization
- **UI Manager Integration** - Tests UI manager initialization in scene context
- **Map Generation** - Tests map generation within actual game scenes
- **Scene Transitions** - Tests scene functionality that requires full Godot engine

#### Phase 3: UI Integration Tests
- **ModernUIManagerTest** - Tests UI manager structure and method signatures
- **InformationPanelTest** - Tests information panel functionality
- **UIScrollingTest** - Tests UI interaction logic and scrolling behavior

### Test Results

#### Expected Results by Phase:
```
=== PHASE 1: NUnit Unit Tests ===
Total Tests: 15
Passed: 15
Failed: 0
Success Rate: 100.0%
🎉 ALL NUnit TESTS PASSED! 🎉

=== PHASE 2: Godot Scene Tests ===
✅ Main Scene Loads Successfully
❌ UI Manager Not Found (actual scene issue)
❌ Map Generation Issues (runtime exceptions)
These tests identify real scene-specific problems!

=== PHASE 3: UI Integration Tests ===
Total Tests: 8
Passed: 8
Failed: 0
Success Rate: 100.0%
🎉 ALL UI TESTS PASSED! 🎉
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

1. **Run Comprehensive Tests**: `./run_tests.sh` (all phases)
2. **Run Individual Phases**: `./run_tests.sh scenes` for scene-specific issues
3. **Check Console Output**: Look for error messages and diagnostic info
4. **Enable Diagnostic Monitor**: Add to scene and press F1
5. **Verify Signal Connections**: Check scene file and inspector
6. **Test Individual Components**: Use specific test scenes to isolate issues

## File Structure

```
Scripts/
├── Main.cs                 # Main game logic
├── HexGridCalculator.cs    # Hex grid calculation utilities
└── [other game scripts]

Scenes/
├── Main.tscn              # Main game scene
├── NUnitTestScene.tscn    # NUnit unit test scene
├── GodotSceneTestScene.tscn # Godot scene test scene
├── UITestScene.tscn       # UI integration test scene
└── [other scenes]

Tests/
├── TestRunner.cs              # NUnit test runner for Godot
├── UITestRunner.cs            # UI test runner
├── GodotSceneTestRunner.cs    # Godot scene test runner
├── HexGridPositionTest.cs     # Unit tests for hex positioning
├── HexGridIntegrationTest.cs  # Integration tests for hex grid
├── IntegrationTests.cs        # Comprehensive game system tests
├── ModernUIManagerTest.cs     # UI manager tests
├── InformationPanelTest.cs    # Information panel tests
├── UIScrollingTest.cs         # UI interaction tests
└── [other test files]

run_tests.sh               # Comprehensive test execution script
TESTING.md                 # This documentation
UI_TESTING.md              # UI testing guide
```

## Best Practices

1. **Always run comprehensive tests** before committing changes: `./run_tests.sh`
2. **Use targeted testing** for specific issues: `./run_tests.sh scenes`
3. **Check diagnostic output** when issues occur
4. **Use descriptive log messages** with clear section markers
5. **Test individual components** before integration
6. **Monitor performance** during development
7. **Leverage scene tests** to catch issues NUnit can't detect

## AI Output Format

The test suite supports an AI-optimized output mode that produces structured, machine-readable results perfect for AI analysis and interpretation.

### Usage
```bash
# Run all tests with AI output
./run_tests.sh --ai-output

# Run specific phase with AI output
./run_tests.sh scenes --ai-output
```

### Output Structure
```
TEST_SUITE_START
PHASE: [phase_name]
BUILD_STATUS: [STARTING|SUCCESS|FAILED]
TESTS_STARTING
[Phase-specific results]
TEST_SUITE_END
```

### Example Output
```
TEST_SUITE_START
PHASE: all
BUILD_STATUS: SUCCESS
PHASE_1_START: NUnit Unit Tests
PHASE_1_RESULTS: PASSED=15 FAILED=0 TOTAL=15
PHASE_1_STATUS: SUCCESS
PHASE_1_END
PHASE_2_START: Godot Scene Tests
PHASE_2_RESULTS: PASSED=1 FAILED=3 TOTAL=4
PHASE_2_ISSUES: UI_MANAGER_NOT_FOUND=2 MAP_GENERATION_FAILED=1 SCENE_LOADED=1
PHASE_2_STATUS: FAILED_WITH_ISSUES
PHASE_2_END
PHASE_3_START: UI Integration Tests
PHASE_3_RESULTS: PASSED=11 FAILED=0 TOTAL=11
PHASE_3_STATUS: SUCCESS
PHASE_3_END
OVERALL_RESULTS: PASSED=27 FAILED=3 TOTAL=30
OVERALL_STATUS: FAILED_WITH_ISSUES
COVERAGE: NUNIT_LOGIC SCENE_INTEGRATION UI_COMPONENTS
TEST_SUITE_END
```

### Key Features
- **Structured Data**: Clear delimiters and key-value format
- **Quantified Results**: Exact test counts and status indicators
- **Issue Tracking**: Specific problem identification (UI_MANAGER_NOT_FOUND, etc.)
- **Coverage Information**: Lists tested functionality types
- **AI-Friendly**: Easy parsing for automated analysis and reporting

## Future Enhancements

- Enhanced scene test coverage for all game scenes
- Performance benchmarking for UI interactions
- Visual test results with screenshots
- Continuous integration setup with multi-phase testing
- Coverage reporting across all test phases
- Automated scene validation tests 