using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Solved.Boss;

/// <summary>
/// Remembers which boss attacks the player has "solved" by perfect-parrying them.
/// The boss AI reads this to stop throwing the clean version of a solved attack,
/// and phase 2 is triggered from here instead of from a health threshold.
/// </summary>
public partial class BossMoveTracker : Node
{
    /// <summary>Perfect parries on one attack before it counts as solved.</summary>
    [Export] public int ParriesToSolve { get; set; } = 2;

    /// <summary>Share of the moveset that must be solved to trigger phase 2.</summary>
    [Export] public float SolvedRatioForPhase2 { get; set; } = 0.6f;

    [Signal] public delegate void MoveSolvedEventHandler(StringName attackId);
    [Signal] public delegate void PhaseTwoTriggeredEventHandler();

    private class MoveRecord
    {
        public int PerfectParries;
        public bool Solved;
        public float AvgParryFrame; // how deep into the parry window the player usually is
    }

    private readonly Dictionary<StringName, MoveRecord> _moves = new();
    private bool _phaseTwo;

    public float SolvedRatio =>
        _moves.Count == 0 ? 0f : (float)_moves.Values.Count(r => r.Solved) / _moves.Count;

    public void RegisterMove(StringName id) => _moves[id] = new MoveRecord();

    public void OnPerfectParry(StringName id, int frameInWindow)
    {
        if (!_moves.TryGetValue(id, out var m)) return;

        m.PerfectParries++;
        m.AvgParryFrame += (frameInWindow - m.AvgParryFrame) / m.PerfectParries;

        if (m.Solved || m.PerfectParries < ParriesToSolve) return;
        m.Solved = true;
        EmitSignal(SignalName.MoveSolved, id);

        if (!_phaseTwo && SolvedRatio >= SolvedRatioForPhase2)
        {
            _phaseTwo = true;
            EmitSignal(SignalName.PhaseTwoTriggered);
        }
    }

    public bool IsSolved(StringName id) => _moves.TryGetValue(id, out var m) && m.Solved;

    public float ParryFrameFor(StringName id) =>
        _moves.TryGetValue(id, out var m) ? m.AvgParryFrame : 0f;
}
