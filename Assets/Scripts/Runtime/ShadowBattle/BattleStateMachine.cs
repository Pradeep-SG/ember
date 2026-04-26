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
        public event Action<int> Resumed; // bg duration ms — listeners re-arm phase clocks

        public void StartBattle(ArchetypeId archetype, string chainId = null, string demonId = null)
        {
            if (Current != BattlePhase.Idle)
                throw new InvalidOperationException($"Cannot StartBattle from {Current}");

            Context = new BattleContext(archetype, new Beads(), chainId, demonId);
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
            // Below threshold: the UI layer shows ResumeOrAbandonSheet and routes user
            // choice to Resume(durMs) or Abandon("user_chose_abandon").
        }

        // Called when the player taps Resume on the sub-60s sheet. Re-arms phase clocks
        // by signaling subscribers (controllers) with the background duration to add to
        // their start anchors. Idle is a no-op.
        public void Resume(int backgroundDurationMs)
        {
            if (Current == BattlePhase.Idle) return;
            Resumed?.Invoke(backgroundDurationMs);
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

            // Resolve outcome BEFORE firing Transitioned so subscribers (e.g.
            // BattleClarityHook) see the final Context.Outcome on the Outcome step.
            if (to == BattlePhase.Outcome && Context != null)
            {
                Context.Outcome = OutcomeRouter.Resolve(Context);

                var completedParams = new Dictionary<string, object>
                {
                    ["battle_id"] = Context.BattleId,
                    ["outcome"] = Context.Outcome.ToString().ToLowerInvariant(),
                    ["phase3_beats_landed"] = Context.BeatsLandedInPhase3,
                    ["final_beads_extinguished"] = Context.DemonBeads != null && Context.DemonBeads.Extinguished,
                };
                if (abandonReason != null) completedParams["abandon_reason"] = abandonReason;
                _emitter.Emit("shadow_battle_completed", completedParams);
                Transitioned?.Invoke(from, to);
                return;
            }

            Transitioned?.Invoke(from, to);

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
                ["battle_id"] = Context != null ? Context.BattleId : null,
                ["phase"] = to.ToString().ToLowerInvariant(),
                ["entry_context"] = entryContext,
            });
        }
    }
}
