using System;
using System.Collections.Generic;
using Kindrith.Dialogue;

namespace Kindrith.ShadowBattle
{
    public sealed class BattleStateMachine
    {
        public const int BackgroundAbandonThresholdMs = 60_000;

        readonly IBattleEventEmitter _emitter;

        public BattleStateMachine(IBattleEventEmitter emitter)
        {
            _emitter = emitter ?? throw new ArgumentNullException(nameof(emitter));
        }

        public BattlePhase Current { get; private set; } = BattlePhase.Idle;
        public BattleContext Context { get; private set; }

        public event Action<BattlePhase, BattlePhase> Transitioned;

        public void StartBattle(ArchetypeId archetype)
        {
            if (Current != BattlePhase.Idle)
                throw new InvalidOperationException($"Cannot StartBattle from {Current}");

            Context = new BattleContext(archetype, new Beads());
            TransitionTo(BattlePhase.Phase1);
        }

        public void Advance()
        {
            var next = NextPhase(Current);
            TransitionTo(next);
            if (next == BattlePhase.Idle)
            {
                Context = null;
            }
        }

        public void Abandon(string reason)
        {
            if (Current == BattlePhase.Idle)
                throw new InvalidOperationException("Cannot Abandon from Idle");

            if (Context != null) Context.Outcome = BattleOutcome.Abandon;
            TransitionTo(BattlePhase.Outcome, abandonReason: reason);
        }

        public void HandleBackgroundResume(int backgroundDurationMs)
        {
            if (Current == BattlePhase.Idle) return;
            if (backgroundDurationMs >= BackgroundAbandonThresholdMs)
            {
                Abandon("background_timeout");
            }
            // Below threshold: production wires the Resume sheet (WP-08).
        }

        static BattlePhase NextPhase(BattlePhase from)
        {
            switch (from)
            {
                case BattlePhase.Phase1: return BattlePhase.Phase2;
                case BattlePhase.Phase2: return BattlePhase.Phase3;
                case BattlePhase.Phase3: return BattlePhase.Outcome;
                case BattlePhase.Outcome: return BattlePhase.Idle;
                default: throw new InvalidOperationException($"Cannot Advance from {from}");
            }
        }

        void TransitionTo(BattlePhase to, string abandonReason = null)
        {
            var from = Current;
            Current = to;
            Transitioned?.Invoke(from, to);

            if (to == BattlePhase.Outcome && Context != null)
            {
                Context.Outcome = OutcomeRouter.Resolve(Context);

                var completedParams = new Dictionary<string, object>
                {
                    ["outcome"] = Context.Outcome.ToString().ToLowerInvariant(),
                    ["beats_landed"] = Context.BeatsLandedInPhase3,
                    ["beads_remaining"] = Context.DemonBeads != null ? Context.DemonBeads.Remaining : 0,
                };
                if (abandonReason != null) completedParams["abandon_reason"] = abandonReason;
                _emitter.Emit("shadow_battle_completed", completedParams);
                return;
            }

            // Idle → don't emit (e.g., on Outcome → Idle wrap-up).
            if (to == BattlePhase.Idle) return;

            var entryContext = new Dictionary<string, object>();
            if (to == BattlePhase.Phase2 && Context != null)
            {
                entryContext["beads_remaining"] = Context.DemonBeads.Remaining;
            }
            if (abandonReason != null)
            {
                entryContext["abandon_reason"] = abandonReason;
            }

            _emitter.Emit("shadow_battle_phase_entered", new Dictionary<string, object>
            {
                ["phase"] = to.ToString(),
                ["entry_context"] = entryContext,
            });
        }
    }
}
