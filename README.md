# SocietyAI

## Overview
SocietyAI is a Unity/C# experimental multi-agent simulation framework that explores emergent behavior through layered AI systems. Instead of hardcoding behaviors like "goblins are cowardly" or "dragons are proud," these characteristics arise organically through simulation.

## Architecture
```text
SocietyAI
    │
    ▼
Creates Patrols
    │
    ▼
BattleAI
    │
    ▼
Combat Results
    │
    ▼
Society Memory
    │
    ▼
Updated Threat & Confidence
    │
    ▼
Next Turn Decisions
```

## Design Goals
The long-term goal is to create living worlds where personalities, strategies, and even legendary individuals emerge naturally from simple rules, bounded memory, and evolving statistics. 

Each Society evaluates neighboring societies using imperfect knowledge, balancing aggression, caution, memory, and confidence to decide whether to expand or consolidate. Patrols are generated from these decisions and resolved through a lightweight BattleAI that learns from combat outcomes rather than relying on scripted behaviors.

## Long-Term Vision
This repository serves as a sandbox for experimenting with emergent AI, autonomous societies, procedural world simulation, and adaptive decision-making that can eventually power dynamic game worlds where each playthrough develops its own unique history.

## Current Features
- ✔ Memory-driven BattleAI
- ✔ Society-level strategic AI
- ✔ Autonomous patrol generation
- ✔ Dynamic threat and confidence learning
- ✔ Emergent combat adaptation

## Planned
- HeroAI
- Multiple societies per race
- Diplomacy
- Economy & resource systems
- Persistent world simulation
