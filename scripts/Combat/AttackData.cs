using Godot;

namespace Solved.Combat;

public enum AttackVariant { Clean, Delayed, Feint }

public enum ParryResult { Hit, Blocked, Perfect }

/// <summary>One boss attack, authored in frames at 60 physics ticks per second.</summary>
[GlobalClass]
public partial class AttackData : Resource
{
    [Export] public StringName Id { get; set; } = "";
    [Export] public int StartupFrames { get; set; } = 24;
    [Export] public int RecoveryFrames { get; set; } = 30;
    [Export] public int Damage { get; set; } = 20;

    /// <summary>Extra startup on the delayed variant during phase 1.</summary>
    [Export] public int DelayedExtraStartup { get; set; } = 14;

    /// <summary>Frame at which the feint variant cancels into the real swing.</summary>
    [Export] public int FeintCancelFrame { get; set; } = 16;
}
