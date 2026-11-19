using System.Collections.Generic;
using System.Linq;
using Godot;
using Solved.Boss;
using Solved.Combat;
using Solved.Player;

namespace Solved.Debug;

/// <summary>
/// Plays the fight the way the mechanic is meant to punish: it parries every attack
/// on the clean version's timing and never adapts. It should solve moves, push the
/// boss into phase 2, and then start eating the delayed and feint variants.
/// Only active when the game is started with <c>-- --autoplay</c>; add
/// <c>--seed=N</c> to pick the boss's rolls. Prints one <c>result</c> line with the
/// run's numbers, then exits with code 0 if phase 2 was reached and 1 if it was not.
/// </summary>
public partial class MemorisedTimingBot : Node
{
    private const string SeedArg = "--seed=";

    [Export] public BossAI Boss { get; set; }
    [Export] public PlayerCombat Player { get; set; }
    [Export] public int AttacksAfterPhaseTwo { get; set; } = 8;
    [Export] public int TickLimit { get; set; } = 20000;

    private readonly HashSet<ulong> _parryTicks = new();
    private readonly int[] _hitsByVariant = new int[3];
    private AttackVariant _variant;
    private bool _feintFollowUp;
    private ulong _seed = 1;
    private ulong _phaseTwoTick;
    private int _attacksLeft = -1;
    private bool _done;

    public override void _Ready()
    {
        string[] args = OS.GetCmdlineUserArgs();
        if (!args.Contains("--autoplay"))
        {
            SetPhysicsProcess(false);
            return;
        }

        string seed = args.FirstOrDefault(a => a.StartsWith(SeedArg));
        if (seed != null) _seed = ulong.Parse(seed[SeedArg.Length..]);

        Boss.Reseed(_seed);
        Boss.AttackStarted += OnAttackStarted;
        Boss.PhaseChanged += OnPhaseChanged;
        Player.Damaged += (_, _) => _hitsByVariant[(int)_variant]++;
        Player.Died += Finish;
    }

    public override void _PhysicsProcess(double delta)
    {
        ulong tick = Engine.GetPhysicsFrames();
        if (_parryTicks.Remove(tick)) Player.TryParry();
        if (tick > (ulong)TickLimit) Finish();
    }

    private void OnAttackStarted(StringName attackId, int variant)
    {
        // The real swing after a feint is announced as Clean; keep counting it as the feint.
        if (_feintFollowUp) _feintFollowUp = false;
        else _variant = (AttackVariant)variant;
        if (_variant == AttackVariant.Feint && variant == (int)AttackVariant.Feint) _feintFollowUp = true;

        // Deliberately ignores the variant: the bot only knows the clean timing.
        AttackData attack = Boss.Moveset.First(a => a.Id == attackId);
        _parryTicks.Add(Engine.GetPhysicsFrames() + (ulong)attack.StartupFrames - 1);

        if (_attacksLeft > 0 && --_attacksLeft == 0) Finish();
    }

    private void OnPhaseChanged(int phase)
    {
        _phaseTwoTick = Engine.GetPhysicsFrames();
        _attacksLeft = AttacksAfterPhaseTwo;
    }

    private void Finish()
    {
        if (_done) return;
        _done = true;

        bool reachedPhaseTwo = Boss.Phase == 2;
        GD.Print($"result seed={_seed} phase2_tick={_phaseTwoTick}"
            + $" hits_clean={_hitsByVariant[(int)AttackVariant.Clean]}"
            + $" hits_delayed={_hitsByVariant[(int)AttackVariant.Delayed]}"
            + $" hits_feint={_hitsByVariant[(int)AttackVariant.Feint]}"
            + $" health={Player.Health} ticks={Engine.GetPhysicsFrames()}");
        GD.Print(reachedPhaseTwo
            ? "autoplay: reached phase 2"
            : "autoplay: FAILED, never reached phase 2");
        GetTree().Quit(reachedPhaseTwo ? 0 : 1);
    }
}
