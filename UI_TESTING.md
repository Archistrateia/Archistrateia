# UI Testing Guide for Archistrateia

## Overview

This document describes the comprehensive UI testing framework implemented for Archistrateia. The framework includes multiple testing approaches to cover UI functionality that NUnit cannot handle, including scene-based testing, UI component testing, and integration testing.

## Testing Framework Components

### 1. **NUnit UI Tests** (Phase 3)
- Tests UI component structure and method signatures
- Validates UI manager functionality via reflection
- Tests information panel behavior and positioning

### 2. **Godot Scene Tests** (Phase 2)
- Tests actual scene loading and initialization
- Validates UI manager integration within scenes
- Tests map generation and scene functionality

### 3. **GodotTestDriver** (Available)
- Provides scene-oriented testing capabilities
- Supports mouse clicks, keyboard input, and UI interactions
- Enables testing within actual Godot scenes

## Running UI Tests

### Comprehensive Test Run (All Phases)
```bash
./run_tests.sh
```

This will run all test phases including UI tests.

### UI Tests Only
```bash
# Run only UI integration tests (Phase 3)
./run_tests.sh ui

# Run only Godot scene tests (Phase 2)
./run_tests.sh scenes

# Run only NUnit unit tests (Phase 1)
./run_tests.sh nunit
```

### Debug Mode for UI Tests
```bash
# Show only failing UI tests with full details
./run_tests.sh ui --show-failures-only

# Combine with AI output for automated analysis
./run_tests.sh ui --show-failures-only --ai-output
```

The `--show-failures-only` option is particularly useful for UI tests since they often have complex initialization issues that are easier to debug when you can focus on just the failures.

### Manual UI Test Run
```bash
# Build the project
dotnet build

# Run UI integration tests
/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --quit-after 20 --main-scene res://Scenes/UITestScene.tscn

# Run Godot scene tests
/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --quit-after 25 --main-scene res://Scenes/GodotSceneTestScene.tscn
```

## UI Test Coverage

### **Phase 3: NUnit UI Tests**

#### **ModernUIManagerTest.cs**
Tests the UI manager structure and API:
- ✅ Class definition and inheritance from Control
- ✅ Required method existence (GetStartGameButton, GetNextPhaseButton, etc.)
- ✅ Required private fields (_startGameButton, _nextPhaseButton, etc.)
- ✅ Method signature validation (UpdatePlayerInfo parameters)

#### **InformationPanelTest.cs**
Tests the information panel functionality:
- ✅ Panel creation and initialization
- ✅ Initial visibility state (hidden)
- ✅ Terrain information display
- ✅ Position updates and viewport bounds handling
- ✅ Unit information display
- ✅ Show/hide functionality
- ✅ Panel node type validation

#### **UIScrollingTest.cs**
Tests UI interaction logic:
- ✅ Scrolling behavior and mouse interaction
- ✅ UI component responsiveness
- ✅ Event handling and state management

### **Phase 2: Godot Scene Tests**

#### **Scene Loading Tests**
- ✅ Main scene loads successfully
- ❌ UI Manager integration issues (identifies real problems)
- ❌ Map generation exceptions (catches runtime errors)

#### **Scene Integration Tests**
- ✅ Scene initialization and setup
- ❌ Component integration problems
- ❌ Scene-specific functionality issues

## Test Structure

### **NUnit UI Tests** (Phase 3)
Tests inherit from `Node` and use reflection to validate UI structure:
```csharp
[TestFixture]
public partial class ModernUIManagerTest : Node
{
    [Test]
    public void ModernUIManager_Should_Have_Required_Methods()
    {
        var type = typeof(ModernUIManager);
        Assert.IsNotNull(type.GetMethod("GetStartGameButton"));
        Assert.IsNotNull(type.GetMethod("UpdatePlayerInfo"));
    }
}
```

### **Godot Scene Tests** (Phase 2)
Tests run within actual Godot scenes to validate real functionality:
```csharp
public partial class GodotSceneTestRunner : Node
{
    private void TestSceneLoading()
    {
        var mainScene = GD.Load<PackedScene>("res://Scenes/Main.tscn");
        Assert.IsNotNull(mainScene, "Main scene should load");
        
        var instance = mainScene.Instantiate();
        Assert.IsNotNull(instance, "Main scene should instantiate");
    }
}
```

## UI Components Tested

### **Buttons**
- Start Game Button
- Next Phase Button
- Regenerate Map Button
- Zoom Controls

### **Interactive Elements**
- Map Type Selector (OptionButton)
- Zoom Slider (HSlider)
- Information Panel
- Game Area mouse interactions

### **Layout Management**
- Top bar positioning
- Right sidebar layout
- Game area clipping
- Responsive panel sizing

### **Visual Feedback**
- Button state changes
- Information panel positioning
- Zoom label updates
- UI hover detection

## Key Testing Features

### **Mouse Simulation**
```csharp
await button.ClickCenter();
await element.GetMouseDriver().MoveToCenter();
await element.GetMouseDriver().ClickAt(position);
```

### **Async UI Interactions**
```csharp
public async void Test_UI_Update()
{
    UpdateUI();
    await WaitForSeconds(0.1f); // Allow UI to update
    Assert.IsTrue(uiUpdated);
}
```

### **Scene Integration**
```csharp
public override void Setup()
{
    base.Setup();
    
    var uiManager = new ModernUIManager();
    AddChild(uiManager);
    uiManager._Ready();
}
```

## Debugging UI Tests

### **Common Issues**
1. **Timing Issues**: Use `WaitForSeconds()` for UI updates
2. **Node References**: Ensure nodes are added to scene before testing
3. **Mouse Events**: Verify `MouseFilter` settings on UI elements

### **Debug Output**
Tests include detailed logging:
```
=== PHASE 2: Godot Scene Tests ===
✅ Main Scene Loads Successfully
❌ UI Manager Not Found (actual scene issue)
❌ Map Generation Issues (runtime exceptions)

=== PHASE 3: UI Integration Tests ===
Running ModernUIManagerTest...
  ✓ ModernUIManager_Should_Be_Defined
  ✓ ModernUIManager_Should_Have_Required_Methods
  ✓ ModernUIManager_Should_Have_Required_Properties
Total Tests: 8
Passed: 8
Failed: 0
Success Rate: 100.0%
🎉 ALL UI TESTS PASSED! 🎉
```

## Best Practices

### **1. Test Isolation**
Each test should be independent and not rely on other tests.

### **2. Async Patterns**
Always use async/await for UI interactions and waits.

### **3. Proper Cleanup**
Tests automatically clean up, but ensure no lingering state.

### **4. Realistic Interactions**
Test actual user interactions (clicks, hovers, drags).

### **5. Edge Cases**
Test viewport bounds, rapid clicking, and error conditions.

## Future Enhancements

- **Visual Regression Testing**: Screenshot comparisons
- **Performance Testing**: UI responsiveness under load
- **Accessibility Testing**: Screen reader and keyboard navigation
- **Cross-Platform Testing**: Different resolutions and input methods
- **Automated CI/CD**: Integration with build pipelines

## Troubleshooting

### **Tests Not Running**
1. Check that packages are installed: `dotnet restore`
2. Verify scene files exist in correct locations
3. Ensure Godot path is correct in test script

### **UI Elements Not Found**
1. Verify nodes are added to scene before testing
2. Check node names and paths
3. Ensure `_Ready()` is called on UI components

### **Timing Issues**
1. Increase `WaitForSeconds()` duration
2. Add explicit waits after UI updates
3. Use `await` for all UI operations

## Integration with Comprehensive Testing

The UI testing framework is part of a comprehensive three-phase testing approach:

### **Phase 1: NUnit Unit Tests**
- Test game logic, calculations, and class functionality
- Fast, isolated tests for core game mechanics
- 100% pass rate expected

### **Phase 2: Godot Scene Tests** 
- Test actual scene loading and UI integration
- Catch scene-specific issues NUnit cannot detect
- Identify real runtime problems and integration issues

### **Phase 3: UI Integration Tests**
- Test UI component structure and behavior
- Validate UI manager functionality via reflection
- Test information panel and interaction logic

This provides comprehensive coverage from low-level logic to high-level scene integration, ensuring both code quality and real-world functionality.
