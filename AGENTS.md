# Working in this repo

Instructions for coding agents, including a Devin Bot assigned to this project.

## Build and run

- Godot 4.4.1 (.NET build) and the .NET 8 SDK.
- Build: `dotnet build Solved.csproj`
- One headless fight: `godot --headless --path . --fixed-fps 60 -- --autoplay --seed=1`
- Seed sweep with a summary table: `GODOT=/path/to/godot tools/sweep.sh 40`

All timings are in physics frames at 60 per second. Attack numbers live in `attacks/*.tres`, parry windows in `scripts/Player/PlayerCombat.cs`.

## What the fight must always do

These hold for the memorised-timing bot (`scripts/Debug/MemorisedTimingBot.cs`), which parries every attack on the clean timing and never adapts. CI checks the first two.

1. It reaches phase 2 in at least 90% of seeds. Phase 2 comes from solved moves, never from boss health.
2. It is never hit by a clean attack. A clean attack that gets through a correctly timed parry is a bug in the frame logic.
3. It does get hit by delayed and feint variants. If those hits drop to zero, the variants have stopped punishing memorised timing.
4. Every variant stays parryable by a player who reads it. A delay shorter than the parry lockout is fine; a hit that lands with no reachable parry window is not.

## Standing jobs for the Bot

- **Every pull request:** run the seed sweep and comment the table. Flag any change in phase 2 reach rate, ticks to phase 2, or hits per variant, and say which changed line caused it.
- **Regressions:** if a rule above breaks, open a pull request with the fix and the before and after tables.
- **Nightly tuning:** sweep the frame values in `attacks/*.tres` (startup, delayed extra startup, feint cancel frame) and the parry windows. Propose a tuning pull request only when it improves a stated target, and put the numbers in the description.
- **New player profiles:** add bots beside the memorised-timing one, starting with one that adapts to variants and one that panic-parries, so the sweep covers more than one playstyle.

Do not merge your own pull requests. Do not change the rules above to make a sweep pass; raise it instead.
