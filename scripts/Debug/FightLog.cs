using Godot;
using Solved.Boss;
using Solved.Combat;
using Solved.Player;

namespace Solved.Debug;

/// <summary>Prints fight events to the console and shows the current state on screen.</summary>
public partial class FightLog : Label
{
    [Export] public BossAI Boss { get; set; }
    [Export] public BossMoveTracker Tracker { get; set; }
    [Export] public PlayerCombat Player { get; set; }

    private string _last = "";

    public override void _Ready()
    {
        Boss.AttackStarted += (id, variant) => Log($"boss: {id} ({(AttackVariant)variant})");
        Boss.Parried += (id, frame) => Log($"perfect parry: {id} on frame {frame}");
        Boss.PhaseChanged += phase => Log($"phase {phase}");
        Tracker.MoveSolved += id => Log($"solved: {id}");
        Player.Damaged += (amount, left) => Log($"player hit for {amount}, {left} left");
        Player.Died += () => Log("player died");
    }

    public override void _Process(double delta)
    {
        Text = $"Phase {Boss.Phase}   Solved {Tracker.SolvedRatio:P0}\n"
             + $"Player {Player.Health}   Boss {Boss.Health}\n"
             + $"{_last}\n\nSpace: parry";
    }

    private void Log(string line)
    {
        _last = line;
        GD.Print($"[{Engine.GetPhysicsFrames()}] {line}");
    }
}
