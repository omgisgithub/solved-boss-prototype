# Solved

[![build](https://github.com/omgisgithub/solved-boss-prototype/actions/workflows/build.yml/badge.svg)](https://github.com/omgisgithub/solved-boss-prototype/actions/workflows/build.yml)

A Godot 4 C# prototype of a souls-like boss fight where the boss learns your parries.

## The mechanic

Perfect-parry the same attack twice and you have "solved" it. The boss stops relying on the clean version of that attack and starts mixing in two variants:

- **Delayed:** the same swing with a longer wind-up, so a parry thrown on the old timing has already ended when the hit lands.
- **Feint:** the swing cancels partway and goes straight into the real one, catching you in parry recovery.

Phase 2 has no health threshold. It starts once you have solved 60% of the moveset. In phase 2:

- Delays are no longer fixed. The boss uses the average frame you parried each attack on and delays by just enough to slip past your window.
- The boss can parry your attacks. The chance scales with how much of the moveset you have solved.

The fight gets harder because you are playing well, and memorised timings stop working.

## Layout

| File | What it does |
| --- | --- |
| `scripts/Boss/BossMoveTracker.cs` | Per-attack parry counts, solved flags, average parry frame. Emits `MoveSolved` and `PhaseTwoTriggered`. |
| `scripts/Boss/BossAI.cs` | Frame-counted boss state machine. Picks an attack and its variant, resolves hits, parries the player in phase 2. |
| `scripts/Player/PlayerCombat.cs` | Parry input and frame windows (perfect, blocked, hit), health, lockout. |
| `scripts/Combat/AttackData.cs` | Attack resource authored in frames, plus the variant and parry-result enums. |
| `scripts/Debug/FightLog.cs` | Prints fight events to the console and shows the current state on screen. |
| `scripts/Debug/MemorisedTimingBot.cs` | Test bot that parries on the clean timing only and never adapts. |
| `attacks/*.tres` | The three test attacks: overhead, sweep, thrust. |
| `Main.tscn` | Test scene wiring the player, tracker, boss, log and bot. |

Everything runs on the physics tick at 60 per second, and all timings are in frames.

## Seeing it work

CI builds the project and plays the fight headless with a bot that parries every attack on the clean version's timing and never adapts. That is the playstyle the mechanic is built to punish. The run has to reach phase 2 to pass.

From one CI run (the number is the physics tick):

```
[40] boss: overhead (Clean)
[72] perfect parry: overhead on frame 1
[162] boss: overhead (Clean)
[194] perfect parry: overhead on frame 1
[194] solved: overhead
...
[422] solved: sweep
[422] phase 2
...
[742] boss: overhead (Delayed)
[781] player hit for 30, 70 left
...
[1202] boss: thrust (Feint)
[1214] boss: thrust (Clean)
```

The bot solves two of the three attacks, which triggers phase 2 with the boss at full health. After that the delayed overhead lands 7 frames later than the clean one and gets through a parry thrown on the old timing.

## Running

Needs Godot 4.4.1 (.NET build) and the .NET 8 SDK. Open the folder in Godot, build, and run. Parry is Space, and the fight state is shown on screen.

To watch the bot play instead:

```
godot --headless --path . --fixed-fps 60 -- --autoplay
```

## Status

Early prototype. The combat logic and a text-only test scene are in. There are no meshes, animations or art yet, and the player can parry but cannot attack or dodge.

Next:

- Placeholder meshes and a real arena scene
- Player attack and dodge, with the boss reacting to perfect dodges as well as parries
- Telegraphs so the delayed and feint variants are readable
- Tuning the frame windows

The plan is to hand these to Devin to build out on top of this prototype.
