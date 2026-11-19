#!/usr/bin/env bash
# Plays the autoplay fight once per seed and prints a Markdown summary table.
# Fails if the bot reaches phase 2 in under 90% of runs or is ever hit by a clean attack.
#
# Usage: tools/sweep.sh [seeds]     (default 40)
# Set GODOT to a Godot 4.4.1 .NET binary if `godot` is not on PATH.
set -euo pipefail

seeds="${1:-40}"
godot="${GODOT:-godot}"
results="$(mktemp)"

for seed in $(seq 1 "$seeds"); do
  "$godot" --headless --path . --fixed-fps 60 -- --autoplay "--seed=$seed" 2>&1 \
    | grep '^result ' >> "$results" || true
done

awk '
{
  for (i = 2; i <= NF; i++) { split($i, kv, "="); v[kv[1]] = kv[2] + 0 }
  runs++
  if (v["phase2_tick"] > 0) {
    reached++
    sum += v["phase2_tick"]
    if (reached == 1 || v["phase2_tick"] < min) min = v["phase2_tick"]
    if (v["phase2_tick"] > max) max = v["phase2_tick"]
  }
  clean += v["hits_clean"]; delayed += v["hits_delayed"]; feint += v["hits_feint"]
  if (v["health"] == 0) deaths++
}
END {
  if (runs == 0) { print "No runs produced a result line."; exit 1 }
  print "| Memorised-timing bot | |"
  print "| --- | --- |"
  printf "| Seeds played | %d |\n", runs
  printf "| Reached phase 2 | %d (%.0f%%) |\n", reached, 100 * reached / runs
  if (reached > 0)
    printf "| Ticks to phase 2, min / avg / max | %d / %.0f / %d |\n", min, sum / reached, max
  printf "| Hits taken from clean / delayed / feint | %d / %d / %d |\n", clean, delayed, feint
  printf "| Bot deaths | %d |\n", deaths
  if (reached / runs < 0.9 || clean > 0) exit 1
}' "$results"
