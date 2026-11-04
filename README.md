# Castle Crash

Castle Crash is a physics-based medieval destruction game inspired by the classic “launch-and-break” gameplay of Angry Birds. The player controls a catapult and must destroy enemy fortifications, towers, and defensive structures belonging to the tyrannical Mad King.

The challenge is not only to hit hard, but to think strategically. Each projectile counts, and buildings are made from different materials with varying strength and durability. Players must identify weak points, exploit structural weaknesses, and use every shot wisely to achieve victory.

## Game Concept

The game revolves around a simple but satisfying loop:

- Aim a catapult at enemy structures
- Launch a limited number of projectiles
- Destroy walls, towers, and defensive elements
- Use strategic shot placement to cause maximum devastation
- Clear the level before running out of ammo

The game blends physics simulation, tactical planning, and medieval combat aesthetics.

## Core Gameplay

### Catapult Controls
The player controls a medieval catapult with adjustable aim and launch power. Precision matters: a well-placed shot can collapse entire sections of a fortress, while a careless throw may waste a valuable projectile.

### Limited Ammunition
Each level has a limited number of projectiles. This forces the player to think ahead, choose the best angle, and make every shot count.

### Destructible Structures
Castles and fortresses are built from different materials such as:
- stone
- wood
- metal
- reinforced blocks

Each material behaves differently and has different durability. Some structures are sturdy and resistant, while others collapse quickly under pressure.

### Strategic Destruction
Success depends on recognizing weak spots and exploiting the environment. Instead of simply attacking the strongest wall, players may need to:
- crack support structures
- destroy key pillars
- cause chain reactions
- collapse large sections through careful timing

## Features

- Physics-based projectile combat
- Medieval fantasy setting
- Castle and fortress destruction mechanics
- Different material properties and durability
- Limited ammo system
- Tactical aiming and precision gameplay
- A satisfying “cause-and-effect” destruction loop

## Current Repository State

This repository currently contains the Unity project foundation for a 2D game prototype, including standard Unity project configuration, URP setup, input system support, and project settings. The gameplay systems, art assets, and custom mechanics are intended to be developed on top of this foundation.

## Project Structure

```text
CastleCrash/
├── Assets/
│   ├── Scenes/
│   └── Settings/
├── Packages/
│   ├── manifest.json
│   └── packages-lock.json
├── ProjectSettings/
│   ├── ProjectVersion.txt
│   └── Unity configuration files
├── .gitignore
├── .gitattributes
├── .vscode/
├── README.md
└── ...
```

## Getting Started

1. Open the project in Unity 6000.3.26f1 or a compatible newer version.
2. Load the main scene from `Assets/Scenes`.
3. Use the Unity editor to playtest the prototype and iterate on gameplay systems.
4. Add your own prefabs, scripts, and level logic under the `Assets` folder.

## Project Management

We use Jira to organize tasks, track sprint progress, and coordinate team development.

- [CastleCrash Jira Board](https://raresvilcu5.atlassian.net/jira/software/projects/SCRUM/boards/1)

*Access to the Jira board requires an invitation to the project.*

## Notes

This project is a prototype foundation for a castle destruction game. It does not yet include a full gameplay implementation beyond the Unity setup and scene scaffolding.
