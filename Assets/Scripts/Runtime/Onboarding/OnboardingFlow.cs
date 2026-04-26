using System;
using System.Collections.Generic;
using System.Linq;
using Kindrith.Analytics;
using Kindrith.Core;
using Kindrith.Data;
using Kindrith.Persistence;

namespace Kindrith.Onboarding
{
    // First-run flow: WardenName → ClassSelect → FirstOath → FirstChain → DemonSummon →
    // TutorialBattle. Idempotent on Resume(): the next-incomplete step is *derived*
    // from store contents (no completion flag) so a kill-and-relaunch lands the player
    // on the same step they were on. Each Submit writes to a store, then re-derives.
    public sealed class OnboardingFlow
    {
        readonly WardenStore _wardens;
        readonly OathStore _oaths;
        readonly ChainStore _chains;
        readonly DemonStore _demons;
        readonly BattleStore _battles;
        readonly AnalyticsBus _analytics;

        DateTime _startedAt;
        DateTime _stepEnteredAt;
        bool _startedEmitted;

        public OnboardingFlow(WardenStore wardens, OathStore oaths, ChainStore chains,
            DemonStore demons, BattleStore battles, AnalyticsBus analytics)
        {
            _wardens = wardens ?? throw new ArgumentNullException(nameof(wardens));
            _oaths = oaths ?? throw new ArgumentNullException(nameof(oaths));
            _chains = chains ?? throw new ArgumentNullException(nameof(chains));
            _demons = demons ?? throw new ArgumentNullException(nameof(demons));
            _battles = battles ?? throw new ArgumentNullException(nameof(battles));
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
            _startedAt = DateTime.UtcNow;
            _stepEnteredAt = _startedAt;
        }

        public OnboardingStepId CurrentStep => DeriveCurrentStep();
        public bool IsComplete => CurrentStep == OnboardingStepId.Complete;

        // First Chain id (lex-first ULID) — the chain whose tutorial battle gates Complete.
        public string OnboardingChainId
        {
            get
            {
                var ids = _chains.ListIds().ToArray();
                Array.Sort(ids, StringComparer.Ordinal);
                return ids.Length > 0 ? ids[0] : null;
            }
        }

        public event Action<OnboardingStepId> StepCompleted;
        public event Action Completed;

        // Re-enters the next-incomplete step. Idempotent — safe to call on every app start.
        // Emits onboarding_started once per OnboardingFlow instance.
        public void Resume()
        {
            EmitStartedIfNeeded();
            _stepEnteredAt = DateTime.UtcNow;
        }

        // Submits the payload for `step` to the appropriate store, then advances if the
        // store now reflects the step as complete. No-op if `step` is not the current step
        // or if the store contents already moved past it.
        public void Submit(OnboardingStepId step, object payload)
        {
            EmitStartedIfNeeded();
            var current = DeriveCurrentStep();
            if (step != current) return;

            switch (step)
            {
                case OnboardingStepId.WardenName: ApplyWardenName(payload as WardenNamePayload); break;
                case OnboardingStepId.ClassSelect: ApplyClassSelect(payload as ClassSelectPayload); break;
                case OnboardingStepId.FirstOath: ApplyFirstOath(payload as FirstOathPayload); break;
                case OnboardingStepId.FirstChain: ApplyFirstChain(payload as FirstChainPayload); break;
                case OnboardingStepId.DemonSummon: ApplyDemonSummon(); break;
                case OnboardingStepId.TutorialBattle: ApplyTutorialBattle(payload as TutorialBattlePayload); break;
            }

            EmitStepCompleted(step);
            StepCompleted?.Invoke(step);
            _stepEnteredAt = DateTime.UtcNow;

            if (DeriveCurrentStep() == OnboardingStepId.Complete)
            {
                EmitCompleted();
                Completed?.Invoke();
            }
        }

        OnboardingStepId DeriveCurrentStep()
        {
            var w = _wardens.LoadOrCreate();
            if (string.IsNullOrEmpty(w.display_name)) return OnboardingStepId.WardenName;
            if (string.IsNullOrEmpty(w.class_id)) return OnboardingStepId.ClassSelect;
            if (!_oaths.ListIds().Any()) return OnboardingStepId.FirstOath;
            if (!_chains.ListIds().Any()) return OnboardingStepId.FirstChain;
            if (!_demons.ListIds().Any()) return OnboardingStepId.DemonSummon;
            var chainId = OnboardingChainId;
            if (chainId == null || !_battles.AnyForChain(chainId)) return OnboardingStepId.TutorialBattle;
            return OnboardingStepId.Complete;
        }

        void ApplyWardenName(WardenNamePayload p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            var name = (p.DisplayName ?? string.Empty).Trim();
            if (name.Length < 2 || name.Length > 24)
                throw new ArgumentException($"DisplayName must be 2..24 chars (got {name.Length})", nameof(p));

            var w = _wardens.LoadOrCreate();
            if (string.IsNullOrEmpty(w.id)) w.id = Ulid.New();
            if (string.IsNullOrEmpty(w.created_at)) w.created_at = DateTime.UtcNow.ToString("o");
            w.display_name = name;
            _wardens.Save(w);
        }

        void ApplyClassSelect(ClassSelectPayload p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            if (p.ClassId != "scholar")
                throw new ArgumentException($"Phase 2 only ships Scholar (got '{p.ClassId}')", nameof(p));
            var w = _wardens.LoadOrCreate();
            w.class_id = p.ClassId;
            _wardens.Save(w);
        }

        void ApplyFirstOath(FirstOathPayload p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            var title = (p.Title ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(title)) throw new ArgumentException("Title required", nameof(p));
            var oath = new OathRecord
            {
                id = Ulid.New(),
                title = title,
                why = (p.Why ?? string.Empty).Trim(),
                class_id = string.IsNullOrEmpty(p.ClassId) ? "scholar" : p.ClassId,
                created_at = DateTime.UtcNow.ToString("o"),
            };
            _oaths.Save(oath.id, oath);
        }

        void ApplyFirstChain(FirstChainPayload p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            var title = (p.Title ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(title)) throw new ArgumentException("Title required", nameof(p));
            var chain = new ChainRecord
            {
                id = Ulid.New(),
                title = title,
                severity_hint = string.IsNullOrEmpty(p.SeverityHint) ? "moderate" : p.SeverityHint,
                created_at = DateTime.UtcNow.ToString("o"),
            };
            _chains.Save(chain.id, chain);
        }

        void ApplyDemonSummon()
        {
            // Generate the Voidwalker demon tied to the first Chain. Tutorial battle uses
            // permission_giver archetype per WP-14 acceptance.
            var chainId = OnboardingChainId;
            if (chainId == null) throw new InvalidOperationException("DemonSummon requires a chain");
            var demon = new DemonRecord
            {
                id = Ulid.New(),
                chain_id = chainId,
                archetype_primary = "permission_giver",
                archetype_pool = new[] { "permission_giver" },
                created_at = DateTime.UtcNow.ToString("o"),
            };
            _demons.Save(demon.id, demon);

            // Stamp demon_id back onto the chain record.
            var chain = _chains.Load(chainId);
            if (chain != null)
            {
                chain.demon_id = demon.id;
                _chains.Save(chainId, chain);
            }
        }

        void ApplyTutorialBattle(TutorialBattlePayload p)
        {
            // No-op; the BattleStore.Save call from BattleRunner already wrote the record.
            // The flow detects completion by AnyForChain(OnboardingChainId).
        }

        void EmitStartedIfNeeded()
        {
            if (_startedEmitted) return;
            // Only emit if there's actually onboarding work to do — a returning player
            // re-binds OnboardingFlow but should not re-fire the event.
            if (DeriveCurrentStep() == OnboardingStepId.Complete) return;
            _analytics.Emit("onboarding_started", new Dictionary<string, object>());
            _startedEmitted = true;
        }

        void EmitStepCompleted(OnboardingStepId step)
        {
            _analytics.Emit("onboarding_step_completed", new Dictionary<string, object>
            {
                ["step_id"] = StepIdName(step),
            });
        }

        void EmitCompleted()
        {
            var totalMs = (long)(DateTime.UtcNow - _startedAt).TotalMilliseconds;
            _analytics.Emit("onboarding_completed", new Dictionary<string, object>
            {
                ["total_duration_ms"] = totalMs,
                ["skipped_steps"] = Array.Empty<string>(),
            });
        }

        static string StepIdName(OnboardingStepId step)
        {
            switch (step)
            {
                case OnboardingStepId.WardenName: return "warden_name";
                case OnboardingStepId.ClassSelect: return "class_select";
                case OnboardingStepId.FirstOath: return "first_oath";
                case OnboardingStepId.FirstChain: return "first_chain";
                case OnboardingStepId.DemonSummon: return "demon_summon";
                case OnboardingStepId.TutorialBattle: return "tutorial_battle";
                case OnboardingStepId.Complete: return "complete";
                default: return step.ToString().ToLowerInvariant();
            }
        }
    }
}
