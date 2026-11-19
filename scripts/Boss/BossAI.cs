using Godot;
using Solved.Combat;
using Solved.Player;

namespace Solved.Boss;

/// <summary>
/// Frame-counted boss state machine. Picks an attack, then picks a variant of it
/// based on what the player has already solved.
/// </summary>
public partial class BossAI : Node
{
    private enum State { Idle, Startup, Recovery, Staggered, Dead }

    [Export] public Godot.Collections.Array<AttackData> Moveset { get; set; } = new();
    [Export] public BossMoveTracker Tracker { get; set; }
    [Export] public PlayerCombat Player { get; set; }

    [Export] public int MaxHealth { get; set; } = 400;
    [Export] public int IdleFrames { get; set; } = 40;
    [Export] public int StaggerFrames { get; set; } = 50;

    /// <summary>How often a solved attack comes out as a delayed or feint variant.</summary>
    [Export] public float SolvedVariantChance { get; set; } = 0.65f;

    /// <summary>Phase 2 only: parry chance against the player at a fully solved moveset.</summary>
    [Export] public float MaxBossParryChance { get; set; } = 0.5f;

    [Signal] public delegate void AttackStartedEventHandler(StringName attackId, int variant);
    [Signal] public delegate void PhaseChangedEventHandler(int phase);
    [Signal] public delegate void DiedEventHandler();

    public int Health { get; private set; }
    public int Phase { get; private set; } = 1;

    private readonly RandomNumberGenerator _rng = new();
    private State _state = State.Idle;
    private int _framesLeft;
    private AttackData _attack;
    private AttackVariant _variant;

    public override void _Ready()
    {
        Health = MaxHealth;
        foreach (var attack in Moveset) Tracker.RegisterMove(attack.Id);
        Tracker.PhaseTwoTriggered += OnPhaseTwoTriggered;
        Enter(State.Idle, IdleFrames);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_state == State.Dead || Moveset.Count == 0) return;
        if (--_framesLeft > 0) return;

        switch (_state)
        {
            case State.Idle:
                BeginAttack(Moveset[_rng.RandiRange(0, Moveset.Count - 1)]);
                break;

            case State.Startup when _variant == AttackVariant.Feint:
                // The swing was a fake. Go straight into the real one while the
                // player is still recovering from the parry they threw at it.
                BeginAttack(_attack, AttackVariant.Clean);
                break;

            case State.Startup:
                ResolveHit();
                break;

            default:
                Enter(State.Idle, IdleFrames);
                break;
        }
    }

    /// <summary>
    /// Called by the player's attack. Returns false if the boss parried it,
    /// which only happens in phase 2 and never while staggered.
    /// </summary>
    public bool TryHit(int damage)
    {
        if (_state == State.Dead) return false;

        bool canParry = Phase == 2 && _state is State.Idle or State.Recovery;
        if (canParry && _rng.Randf() < Tracker.SolvedRatio * MaxBossParryChance)
        {
            Player.OnAttackParried();
            return false;
        }

        Health -= damage;
        if (Health <= 0)
        {
            _state = State.Dead;
            EmitSignal(SignalName.Died);
        }
        return true;
    }

    private void BeginAttack(AttackData attack, AttackVariant? forced = null)
    {
        _attack = attack;
        _variant = forced ?? PickVariant(attack);

        int startup = _variant switch
        {
            AttackVariant.Delayed => attack.StartupFrames + DelayFor(attack),
            AttackVariant.Feint => attack.FeintCancelFrame,
            _ => attack.StartupFrames,
        };

        Enter(State.Startup, startup);
        EmitSignal(SignalName.AttackStarted, attack.Id, (int)_variant);
    }

    private AttackVariant PickVariant(AttackData attack)
    {
        if (!Tracker.IsSolved(attack.Id)) return AttackVariant.Clean;
        if (_rng.Randf() > SolvedVariantChance) return AttackVariant.Clean;
        return _rng.Randf() < 0.5f ? AttackVariant.Delayed : AttackVariant.Feint;
    }

    private int DelayFor(AttackData attack)
    {
        if (Phase == 1) return attack.DelayedExtraStartup;

        // Phase 2: delay just long enough that a parry thrown on the player's
        // usual timing has already run out of active frames when the hit lands.
        int usualFrame = Mathf.FloorToInt(Tracker.ParryFrameFor(attack.Id));
        return Mathf.Max(1, Player.ParryActiveFrames - usualFrame);
    }

    private void ResolveHit()
    {
        switch (Player.ResolveIncomingHit(out int frameInWindow))
        {
            case ParryResult.Perfect:
                Tracker.OnPerfectParry(_attack.Id, frameInWindow);
                Enter(State.Staggered, StaggerFrames);
                return;

            case ParryResult.Blocked:
                Player.TakeDamage(_attack.Damage / 4);
                break;

            default:
                Player.TakeDamage(_attack.Damage);
                break;
        }

        Enter(State.Recovery, _attack.RecoveryFrames);
    }

    private void OnPhaseTwoTriggered()
    {
        Phase = 2;
        EmitSignal(SignalName.PhaseChanged, Phase);
    }

    private void Enter(State state, int frames)
    {
        _state = state;
        _framesLeft = frames;
    }
}
