# Tetris 3D

A modern 3D Tetris implementation built with Unity 6 and URP, featuring clean architecture, advanced visual effects, and polished gameplay.

![Unity](https://img.shields.io/badge/Unity-6-black?logo=unity)
![URP](https://img.shields.io/badge/URP-17+-blue)
![C#](https://img.shields.io/badge/C%23-9.0-purple)

## Screenshots

<p align="center">
  <img src="ScreenShoots/Screenshot_1.png" width="32%" />
  <img src="ScreenShoots/Screenshot_2.png" width="32%" />
  <img src="ScreenShoots/Screenshot_3.png" width="32%" />
</p>

## Download

**[Download for Windows](Windows/Tetris3D.exe)** — Ready-to-play build

## Overview

This project extends classic Tetris into 3D space with full rotation on three axes, an orbital camera system, and underwater-themed visual effects. Built with a focus on clean code architecture and professional game development practices.

## Features

### Gameplay
- **3D Tetris mechanics** with X, Y, Z rotation axes
- **Classic + custom pieces** including 3D-specific shapes
- **Ghost piece preview** showing landing position
- **Progressive difficulty** with level-based gravity scaling
- **Combo scoring system** with multipliers

### Visual Effects
- **Water distortion** — screen-space wave effect triggered by piece movement and line clears
- **Caustics rendering** — procedural light patterns applied via directional light cookies
- **Cascade animations** — smooth block falling with easing curves
- **Camera shake** — impact feedback on hard drops

### Controls
- **Orbital camera** with mouse-based rotation and zoom
- **Camera-relative movement** — input direction adapts to camera facing
- **Key repeat** with configurable delay for responsive controls

## Architecture

The project follows a layered architecture with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────┐
│                      Bootstrap                          │
│              (Dependency Injection & Setup)             │
└─────────────────────────────────────────────────────────┘
        │           │           │           │
        ▼           ▼           ▼           ▼
┌───────────┐ ┌───────────┐ ┌───────────┐ ┌───────────┐
│    UI     │ │Presentation│ │ Rendering │ │   Input   │
│  Screens  │ │   Views   │ │  Effects  │ │  Sources  │
└───────────┘ └───────────┘ └───────────┘ └───────────┘
        │           │           │           │
        └───────────┴─────┬─────┴───────────┘
                          ▼
              ┌───────────────────────┐
              │         Core          │
              │  (Game Logic & State) │
              └───────────────────────┘
```

### Core Layer
Framework-agnostic game logic:
- **State Machine** — 7 states managing game flow (MainMenu → Spawning → Falling → Locking → LineClearing → Paused → GameOver)
- **Command Pattern** — decoupled input handling via ICommand interface
- **Event System** — observer pattern for loose coupling between systems
- **3D Field** — collision detection and line clearing in 3D space

### Presentation Layer
Visual representation:
- **Object Pooling** — pre-allocated block instances to eliminate GC spikes
- **Orbital Camera** — spherical coordinate system with smooth damping
- **Animation System** — DOTween integration for movement and effects

### Rendering Layer
URP-based visual effects:
- **Water Distortion** — post-process shader supporting 4 simultaneous waves
- **Caustics** — procedural texture rendered to light cookie each frame

### Input Layer
Responsive controls:
- **Camera-Aware Mapping** — movement directions remap based on camera yaw
- **Key Repeat System** — initial delay + repeat interval for held keys

## Technical Highlights

| Feature | Implementation |
|---------|----------------|
| 3D Rotation | Quaternion-based with discrete 90° steps around world axes |
| Piece Spawning | Bounding box centering with configurable spawn offset |
| Line Detection | Per-layer fullness check across X and Z dimensions |
| Camera Shake | Exponential decay with intensity proportional to drop distance |
| Wave Effect | World-to-screen UV projection with amplitude decay curves |

## Design Patterns

- **Finite State Machine** — game flow control with transition validation
- **Command** — encapsulated player actions (Move, Rotate, HardDrop, Pause)
- **Observer** — event-driven communication between layers
- **Object Pool** — reusable block instances with warmup
- **Dependency Injection** — bootstrapper wires all dependencies at startup

## Configuration

All gameplay parameters are exposed via ScriptableObjects:

- `GameFieldSettings` — field dimensions (X, Y, Z)
- `GravitySettings` — fall intervals per level, lock delay, soft drop multiplier
- `ScoreSettings` — points per line, combo multipliers, lines per level
- `CameraSettings` — rotation sensitivity, distance limits, shake parameters
- `KeyBindings` — customizable input mapping

## Project Structure

```
Assets/_Project/
├── Code/
│   ├── Tetris.Core/          # Game logic, state machine, commands
│   ├── Tetris.Input/         # Input handling, camera orientation
│   ├── Tetris.Presentation/  # Views, pooling, camera, animations
│   ├── Tetris.Rendering/     # Water distortion, caustics
│   ├── Tetris.UI/            # Screens, HUD, controllers
│   └── Tetris.Bootstrap/     # Dependency injection, initialization
├── Configs/                  # ScriptableObject assets
├── Prefabs/                  # Block prefab, UI elements
├── Art/                      # Shaders, materials
└── Scenes/                   # Game scene
```

## Requirements

- Unity 6.0+
- Universal Render Pipeline 17+
- DOTween Pro

## Getting Started

1. Clone the repository
2. Open project in Unity 6
3. Open `Assets/_Project/Scenes/Game.unity`
4. Enter Play Mode

## Controls

| Action | Key |
|--------|-----|
| Move | Arrow Keys / WASD |
| Rotate X | Q / E |
| Rotate Y | R / F |
| Rotate Z | Z / X |
| Soft Drop | Down Arrow (hold) |
| Hard Drop | Space |
| Pause | Escape |
| Camera | Mouse Drag + Scroll |

## What I Learned

This project was a learning experience in several areas:

### State Machine Architecture
First time implementing a **Finite State Machine** in a real project. Learned how to structure game flow through distinct states (MainMenu, Falling, Locking, LineClearing, etc.) with clean transitions and shared context. This pattern made the game logic predictable and easy to extend.

### Shaders and Shader Graph
Explored **URP Shader Graph** for creating visual effects. Built custom shaders for:
- Water distortion post-processing effect
- Block materials with proper lighting response
- Ghost piece transparency

### Post-Processing and Render Pipeline
Learned to work with **URP Render Features** and **RenderGraph API**:
- Creating custom render passes that inject into the pipeline
- Screen-space effects with proper UV calculations
- Managing multiple simultaneous wave effects with shader globals

### Caustics Effect
Implemented a **procedural caustics system** similar to AAA underwater games:
- Rendering caustics pattern to a RenderTexture each frame
- Applying the texture as a light cookie on the directional light
- Achieving realistic underwater lighting without baked lightmaps

### Combining Architectural Patterns
Practiced combining multiple **design patterns** into a cohesive architecture:
- Command pattern for input decoupling
- Observer pattern for event-driven communication
- Object pooling for performance
- Dependency injection for testability

The challenge was making these patterns work together without over-engineering, keeping the codebase maintainable while achieving clean separation of concerns.

## License

This project is available for portfolio and educational purposes.

---

*Built with Unity 6 and URP*
