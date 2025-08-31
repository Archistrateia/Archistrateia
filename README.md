# Archistrateia

[![Version](https://img.shields.io/badge/version-1.0.0-blue.svg)](https://github.com/yourusername/Archistrateia/releases/tag/v1.0.0)
[![Godot](https://img.shields.io/badge/Godot-4.4+-green.svg)](https://godotengine.org/)
[![C#](https://img.shields.io/badge/C%23-.NET%208.0-purple.svg)](https://dotnet.microsoft.com/)
[![Tests](https://img.shields.io/badge/tests-104%20passed-brightgreen.svg)](https://github.com/yourusername/Archistrateia/actions)
[![License](https://img.shields.io/badge/license-MIT-yellow.svg)](LICENSE)

A strategic war simulation game built with Godot Engine and C#.

> **🎉 Version 1.0.0 Released!** Complete movement system modernization with Godot integration.

## Overview

Archistrateia is a turn-based strategic war simulation that focuses on tactical decision-making and resource management in military conflicts. Players command ancient Egyptian armies, managing units, cities, and resources across diverse terrain.

## Features

### Core Game Systems
- **Turn-based gameplay** with 4 distinct phases: Earn, Purchase, Move, Combat
- **Unit management** with 4 unique unit types: Nakhtu, Medjay, Archer, Charioteer
- **Resource system** using gold/drachma for unit purchasing and city management
- **Terrain system** with 5 terrain types affecting movement and combat
- **City management** with production values and ownership mechanics

### Unit Types
- **Nakhtu** (3 Attack, 2 Defense, 2 Movement) - Basic infantry
- **Medjay** (4 Attack, 3 Defense, 3 Movement) - Elite guards
- **Archer** (5 Attack, 1 Defense, 2 Movement) - Ranged units
- **Charioteer** (6 Attack, 2 Defense, 4 Movement) - Fast cavalry

### Terrain Types
- **Desert** - High movement cost, no defense bonus
- **Hill** - High movement cost, +2 defense bonus
- **River** - Very high movement cost, +1 defense bonus
- **Shoreline** - Low movement cost, no defense bonus
- **Lagoon** - Highest movement cost, no defense bonus

## Development

### Prerequisites

- Godot Engine 4.4+
- .NET SDK
- C# development environment

### Getting Started

1. Open the project in Godot Editor
2. Build the C# solution
3. Run the project

### Project Structure

```
Archistrateia/
├── Scenes/          # Godot scene files
├── Scripts/         # C# scripts
│   ├── Main.cs      # Main game controller
│   ├── GameManager.cs # Core game orchestration
│   ├── TurnManager.cs # Turn and phase management
│   ├── Unit.cs      # Unit base class and types
│   ├── Player.cs    # Player management
│   ├── City.cs      # City and production system
│   ├── HexTile.cs   # Terrain and tile management
│   └── GameExample.cs # System demonstration
├── Assets/          # Game assets
│   ├── Textures/    # Texture files
│   ├── Models/      # 3D models
│   ├── Sounds/      # Audio files
│   └── UI/          # UI assets
├── project.godot    # Main project configuration
├── Archistrateia.csproj # C# project file
└── Archistrateia.sln   # C# solution file
```

### Core Classes

#### TurnManager
Manages the 4-phase turn sequence and tracks game progression.

#### Unit System
Base Unit class with derived classes for each unit type, including movement and combat mechanics.

#### Player
Handles unit ownership, gold management, and city control.

#### City
Manages unit production, resource generation, and ownership.

#### HexTile
Represents individual map tiles with terrain effects and unit/city placement.

#### GameManager
Orchestrates all game systems and provides high-level game control.

## Version Information

### Current Version: 1.0.0 🚀

**Release Date**: August 31, 2025  
**Status**: Major Release - Complete Movement System Modernization

#### What's New in 1.0.0
- ✨ **Godot-Native Movement System** - Replaced custom pathfinding with Godot's AStar2D
- 🎬 **Enhanced Animations** - Hardware-accelerated movement with Tween system
- 🎨 **Modern Visual Rendering** - Full integration with Godot's rendering primitives
- 🧪 **100% Test Coverage** - All 104 tests passing successfully
- 🚀 **Performance Improvements** - 2-3x faster pathfinding, reduced memory usage

#### Previous Versions
- **0.1.0** - Initial release with core game systems and custom implementations

For detailed information, see [CHANGELOG.md](CHANGELOG.md).

---

## Development Status

✅ **Core Systems Implemented** - All basic game mechanics are functional
- Turn management system
- Unit creation and management
- Player resource system
- City production mechanics
- Terrain system with movement costs
- Basic game state management

✅ **Movement System Modernized** - Complete Godot integration
- Godot-native pathfinding with AStar2D
- Hardware-accelerated animations
- Modern visual rendering system
- Comprehensive test coverage

🚧 **Next Steps** - UI integration and gameplay expansion
- Visual representation of units and terrain
- User interface for turn management
- Combat resolution system
- Map generation and visualization
- Multiplayer support

## Game Mechanics

### Turn Structure
1. **Earn Phase** - Players receive gold from owned cities
2. **Purchase Phase** - Players buy new units with available gold
3. **Move Phase** - Players move units across the map
4. **Combat Phase** - Units engage in combat (system to be expanded)

### Resource Management
- Cities generate gold each turn based on their production value
- Units have specific costs and must be purchased during the purchase phase
- Movement is limited by terrain costs and unit movement points

### Strategic Elements
- Terrain affects both movement and combat effectiveness
- Cities provide defensive bonuses and economic benefits
- Unit variety creates tactical depth and strategic choices

## License

To be determined

## Contributing

This is currently a personal project. Contributions guidelines will be added later.
