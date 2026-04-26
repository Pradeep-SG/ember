using System;
using Kindrith.Dialogue;

namespace Kindrith.ShadowBattle
{
    // Wires Phase 2 dialogue choices and the final outcome into Resonance / Clarity
    // side-effects per phase1-shadow-battle-spec.md §3.2 and resonance-spec.md.
    //   Counter chosen → +0.1 ClarityPool (clamped 0..1.0)
    //   Agree   chosen → DemonBeads.Regen(1) (clamped to Beads.Max)
    //   Win     resolved → onClarityBuffEarned() fires so the UI/Persistence layer
    //                       can stamp a 24h clarity_expires_at on WardenRecord.
    public sealed class BattleClarityHook : IDisposable
    {
        public const float ClarityPerCounter = 0.1f;
        public const int RegenPerAgree = 1;

        readonly BattleContext _context;
        readonly DialogueRunner _runner;
        readonly BattleStateMachine _sm;
        readonly Action _onClarityBuffEarned;

        bool _subscribed;
        bool _buffEarned;

        public BattleClarityHook(
            BattleContext context,
            DialogueRunner runner,
            BattleStateMachine sm,
            Action onClarityBuffEarned)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _sm = sm ?? throw new ArgumentNullException(nameof(sm));
            _onClarityBuffEarned = onClarityBuffEarned ?? throw new ArgumentNullException(nameof(onClarityBuffEarned));

            _runner.OptionChosen += OnOptionChosen;
            _sm.Transitioned += OnTransitioned;
            _subscribed = true;
        }

        public bool ClarityBuffEarned => _buffEarned;

        void OnOptionChosen(DialogueOption option, OptionClass cls)
        {
            switch (cls)
            {
                case OptionClass.Counter:
                    _context.ClarityPool = Math.Min(1.0f, _context.ClarityPool + ClarityPerCounter);
                    break;
                case OptionClass.Agree:
                    _context.DemonBeads.Regen(RegenPerAgree);
                    break;
            }
        }

        void OnTransitioned(BattlePhase from, BattlePhase to)
        {
            if (to != BattlePhase.Outcome) return;
            // BattleStateMachine.TransitionTo resolves Context.Outcome before firing
            // Transitioned for the Outcome step, so the read below is final.
            if (_buffEarned) return;
            if (_context.Outcome == BattleOutcome.Win || _context.Outcome == BattleOutcome.CriticalWin)
            {
                _buffEarned = true;
                _onClarityBuffEarned();
            }
        }

        public void Dispose()
        {
            if (!_subscribed) return;
            _runner.OptionChosen -= OnOptionChosen;
            _sm.Transitioned -= OnTransitioned;
            _subscribed = false;
        }
    }
}
