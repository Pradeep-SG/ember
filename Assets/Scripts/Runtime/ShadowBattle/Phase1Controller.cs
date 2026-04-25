using System;
using System.Collections.Generic;
using Kindrith.Breathing;
using Kindrith.Core;

namespace Kindrith.ShadowBattle
{
    public sealed class Phase1Controller
    {
        public const int MaxDurationMs = 90_000;

        readonly IClock _clock;
        readonly BreathingClock _breathingClock;
        readonly TapEvaluator _evaluator;
        readonly Beads _beads;
        readonly BattleContext _context;
        readonly IBattleEventEmitter _emitter;
        readonly Action _onComplete;

        long _startMs;
        bool _running;

        public Phase1Controller(
            IClock clock,
            BreathingClock breathingClock,
            TapEvaluator evaluator,
            Beads beads,
            BattleContext context,
            IBattleEventEmitter emitter,
            Action onComplete)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _breathingClock = breathingClock ?? throw new ArgumentNullException(nameof(breathingClock));
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            _beads = beads ?? throw new ArgumentNullException(nameof(beads));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _emitter = emitter ?? throw new ArgumentNullException(nameof(emitter));
            _onComplete = onComplete ?? throw new ArgumentNullException(nameof(onComplete));
        }

        public bool IsRunning => _running;

        public void Start()
        {
            _startMs = _clock.NowMs;
            _running = true;
        }

        // Re-arm after a background pause so elapsed-since-Start excludes the bg duration.
        public void Resume(int backgroundDurationMs)
        {
            if (!_running) return;
            _startMs += backgroundDurationMs;
        }

        public void Tick()
        {
            if (!_running) return;

            var elapsedMs = (int)(_clock.NowMs - _startMs);
            _context.Phase1ElapsedMs = elapsedMs;

            if (elapsedMs >= MaxDurationMs || _beads.Extinguished)
            {
                _running = false;
                _onComplete();
            }
        }

        public void RegisterTap()
        {
            if (!_running) return;

            var grade = _evaluator.Grade(_breathingClock.CycleElapsedMs, out int offsetMs);
            var damage = TapEvaluator.DamageFor(grade);
            _beads.Damage(damage);

            _emitter.Emit("shadow_battle_tap_registered", new Dictionary<string, object>
            {
                ["battle_id"] = _context.BattleId,
                ["grade"] = grade.ToString().ToLowerInvariant(),
                ["offset_ms"] = offsetMs,
                ["cycle_index"] = _breathingClock.CycleIndex,
            });
        }
    }
}
