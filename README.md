# Solved

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

Everything runs on the physics tick at 60 per second, and all timings are in frames.

## Status

Early scaffold. The combat logic is scripts only: there are no scenes, animations or art yet, and the scripts have not been run in the editor.

Next:

- A test scene with a player, a boss and placeholder meshes
- Player attack and dodge, with the boss reacting to perfect dodges as well as parries
- Telegraphs so the delayed and feint variants are readable
- Tuning the frame windows

The plan is to hand these to Devin to build out on top of this scaffold.

## Running

Needs Godot 4.4 (.NET build) and the .NET 8 SDK. Open the folder in Godot and build the C# solution. Parry is bound to Space by default.
