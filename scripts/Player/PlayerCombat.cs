using Godot;
using Solved.Combat;

namespace Solved.Player;

/// <summary>
/// Player-side frame logic: when the parry was pressed, and how an incoming hit
/// resolves against it.
/// </summary>
public partial class PlayerCombat : Node
{
    private const string ParryAction = "parry";

    /// <summary>Frames at the start of the parry that count as perfect.</summary>
    [Export] public int PerfectFrames { get; set; } = 3;

    /// <summary>Total frames the parry blocks at all.</summary>
    [Export] public int ParryActiveFrames { get; set; } = 8;

    /// <summary>Frames after the parry ends before another can start.</summary>
    [Export] public int ParryRecoveryFrames { get; set; } = 14;

    /// <summary>Lockout after the boss parries one of the player's attacks.</summary>
    [Export] public int ParriedLockoutFrames { get; set; } = 30;

    [Export] public int MaxHealth { get; set; } = 100;

    [Signal] public delegate void DamagedEventHandler(int amount, int healthLeft);
    [Signal] public delegate void DiedEventHandler();

    public int Health { get; private set; }

    private long _parryStart = long.MinValue / 2;
    private long _lockedUntil;

    private static long Tick => (long)Engine.GetPhysicsFrames();

    public override void _Ready()
    {
        Health = MaxHealth;

        // Default binding so the project runs without editing the input map.
        if (!InputMap.HasAction(ParryAction))
        {
            InputMap.AddAction(ParryAction);
            InputMap.ActionAddEvent(ParryAction, new InputEventKey { PhysicalKeycode = Key.Space });
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Input.IsActionJustPressed(ParryAction)) TryParry();
    }

    public bool TryParry()
    {
        long tick = Tick;
        if (tick < _lockedUntil) return false;

        _parryStart = tick;
        _lockedUntil = tick + ParryActiveFrames + ParryRecoveryFrames;
        return true;
    }

    /// <summary>
    /// Resolves a boss hit landing this tick. <paramref name="frameInWindow"/> is how
    /// many frames into the parry the hit landed, or -1 if no parry was active.
    /// </summary>
    public ParryResult ResolveIncomingHit(out int frameInWindow)
    {
        long sinceParry = Tick - _parryStart;
        if (sinceParry < 0 || sinceParry >= ParryActiveFrames)
        {
            frameInWindow = -1;
            return ParryResult.Hit;
        }

        frameInWindow = (int)sinceParry;
        return frameInWindow < PerfectFrames ? ParryResult.Perfect : ParryResult.Blocked;
    }

    public void TakeDamage(int amount)
    {
        if (Health <= 0) return;

        Health = Mathf.Max(0, Health - amount);
        EmitSignal(SignalName.Damaged, amount, Health);
        if (Health == 0) EmitSignal(SignalName.Died);
    }

    public void OnAttackParried() => _lockedUntil = Tick + ParriedLockoutFrames;
}
