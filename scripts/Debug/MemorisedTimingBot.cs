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
/// Only active when the game is started with <c>-- --autoplay</c>. Exits with code 0
/// if phase 2 was reached and 1 if it was not.
/// </summary>
public partial class MemorisedTimingBot : Node
{
    [Export] public BossAI Boss { get; set; }
    [Export] public PlayerCombat Player { get; set; }
    [Export] public int AttacksAfterPhaseTwo { get; set; } = 8;
    [Export] public int TickLimit { get; set; } = 20000;

    private readonly HashSet<ulong> _parryTicks = new();
    private int _attacksLeft = -1;
    private bool _done;

    public override void _Ready()
    {
        if (!OS.GetCmdlineUserArgs().Contains("--autoplay"))
        {
            SetPhysicsProcess(false);
            return;
        }

        Boss.Reseed(1);
        Boss.AttackStarted += OnAttackStarted;
        Boss.PhaseChanged += _ => _attacksLeft = AttacksAfterPhaseTwo;
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
        // Deliberately ignores the variant: the bot only knows the clean timing.
        AttackData attack = Boss.Moveset.First(a => a.Id == attackId);
        _parryTicks.Add(Engine.GetPhysicsFrames() + (ulong)attack.StartupFrames - 1);

        if (_attacksLeft > 0 && --_attacksLeft == 0) Finish();
    }

    private void Finish()
    {
        if (_done) return;
        _done = true;

        bool reachedPhaseTwo = Boss.Phase == 2;
        GD.Print(reachedPhaseTwo
            ? "autoplay: reached phase 2"
            : "autoplay: FAILED, never reached phase 2");
        GetTree().Quit(reachedPhaseTwo ? 0 : 1);
    }
}
