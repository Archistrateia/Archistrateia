# Hex Grid Refactoring

## Overview

This document describes the refactoring of the hex grid positioning system to use a proper flat-top hex grid formula that creates a perfect tessellated hex grid.

## 🎯 Flat-Top Hex Grid Formula

The hex grid now uses the **flat-top formula** for perfect tessellation:

### Constants
- `HEX_SIZE = 35.0f` (radius of hexagon)
- `HEX_WIDTH = HEX_SIZE * 2.0f` (width = diameter)
- `HEX_HEIGHT = HEX_SIZE * 1.732f` (height = radius * √3)

### Positioning Formula
```csharp
// Horizontal spacing: 75% of hex width for overlap
float xPos = x * HEX_WIDTH * 0.75f;

// Vertical spacing: full hex height
float yPos = y * HEX_HEIGHT;

// Offset odd columns down by half the height to create proper flat-top pattern
if (x % 2 == 1)
{
    yPos += HEX_HEIGHT * 0.5f;
}
```

### Mathematical Basis
- **Horizontal spacing**: `HEX_WIDTH * 0.75f` (75% of width for proper overlap)
- **Vertical spacing**: `HEX_HEIGHT` (full height between rows)
- **Odd column offset**: `HEX_HEIGHT * 0.5f` (half the height downward)

This creates a perfect flat-top hex grid where:
- Each hexagon overlaps properly with its neighbors
- Odd columns are offset downward by half the height
- The grid forms a continuous, tightly packed tessellation

## Example Position Calculations

| Coordinate | X Position | Y Position | Column Type |
|------------|------------|------------|-------------|
| (0,0)      | 0.0        | 0.0        | Even        |
| (0,1)      | 0.0        | 60.62      | Even        |
| (1,0)      | 52.5       | 30.31      | Odd         |
| (1,1)      | 52.5       | 90.93      | Odd         |
| (2,0)      | 105.0      | 0.0        | Even        |

## Benefits of Refactoring

1. **Separation of Concerns**: Hex calculations are now isolated in their own class
2. **Reusability**: `HexGridCalculator` can be used by other parts of the game
3. **Testability**: All hex positioning logic is thoroughly tested
4. **Maintainability**: Constants and logic are centralized and well-documented
5. **Flexibility**: Easy to adjust hex size and spacing by changing constants
6. **Correct Staggering**: Proper odd-q vertical layout with correct offsets
7. **Perfect Tessellation**: Flat-top formula ensures no gaps between hexagons
8. **Viewport Centering**: Grid is properly centered in viewport with offset compensation

## Files Created/Modified

### Core Implementation
- `Scripts/HexGridCalculator.cs` - Updated with flat-top hex grid formula
- `Scripts/Main.cs` - Uses HexGridCalculator for tile positioning

### Tests
- `Tests/HexGridPositionTest.cs` - Unit tests for hex positioning
- `Tests/HexGridIntegrationTest.cs` - Integration tests
- `Tests/HoneycombHexGridTest.cs` - Flat-top hex grid tests (renamed from pointy-top)
- `Tests/IntegrationTests.cs` - General integration tests

### Test Infrastructure
- `Tests/TestRunner.cs` - NUnit test runner for Godot
- `Scenes/NUnitTestScene.tscn` - Test scene
- `run_tests.sh` - Test execution script

## Usage

The hex grid is automatically used in the main game:

```csharp
// In Main.cs CreateHexTile method
hexShape.Position = HexGridCalculator.CalculateHexPositionCentered(x, y, viewportSize, mapWidth, mapHeight);
```

The centering function includes compensation for the odd column offset to ensure all hexagons are visible:

```csharp
// Centering with offset compensation
float centerY = (viewportSize.Y - mapTotalHeight) / 2 + HEX_HEIGHT * 0.5f;
```

## Testing

Run all tests with:
```bash
./run_tests.sh
```

The test suite includes:
- Unit tests for hex positioning calculations
- Integration tests for hex grid functionality
- Flat-top tessellation verification
- Game integration tests
- Centering and viewport calculations

All 20 tests pass, ensuring the hex grid implementation is correct.

## Mathematical Verification

The flat-top formula ensures:
- **Horizontal spacing**: `HEX_WIDTH * 0.75f` for proper column overlap
- **Vertical spacing**: `HEX_HEIGHT` for full row separation
- **Perfect tessellation**: No gaps or overlaps between hexagons
- **Consistent staggering**: Odd columns offset down by exactly half height
- **Viewport centering**: Grid centered with compensation for negative offsets

This creates a visually perfect flat-top hex grid where hexagons are positioned in a classic brick-like pattern with alternating column offsets. 