using System;
using System.Collections.Generic;
using Kindrith.Core;

namespace Kindrith.ShadowBattle
{
    public sealed class Phase3Controller
    {
        public const int MaxDurationMs = 30_000;

        readonly IClock _clock;
        readonly FinisherSequencer _sequencer;
        readonly BattleContext _context;
        readonly IBattleEventEmitter _emitter;
        readonly Action _onComplete;

        long _startMs;
        bool _running;

        public Phase3Controller(
            IClock clock,
            FinisherSequencer sequencer,
            BattleContext context,
            IBattleEventEmitter emitter,
            Action onComplete)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _sequencer = sequencer ?? throw new ArgumentNullException(nameof(sequencer));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _emitter = emitter ?? throw new ArgumentNullException(nameof(emitter));
            _onComplete = onComplete ?? throw new ArgumentNullException(nameof(onComplete));
        }

        public bool IsRunning => _running;
        public FinisherSequencer Sequencer => _sequencer;

        public void Start()
        {
            if (_running) return;
            _startMs = _clock.NowMs;
            _running = true;
            _sequencer.BeatResolved += OnBeatResolved;
            _sequencer.Start();
        }

        // Re-arm after a background pause so elapsed-since-Start excludes the bg duration.
        public void Resume(int backgroundDurationMs)
        {
            if (!_running) return;
            _startMs += backgroundDurationMs;
            _sequencer.Resume(backgroundDurationMs);
        }

        public void Tick()
        {
            if (!_running) return;
            _sequencer.Tick();
            if (_sequencer.IsComplete)
            {
                Finish();
                return;
            }
            var elapsedMs = (int)(_clock.NowMs - _startMs);
            if (elapsedMs >= MaxDurationMs)
            {
                Finish();
            }
        }

        void OnBeatResolved(FinisherBeat beat, bool landed, int offsetMs)
        {
            _emitter.Emit("shadow_battle_finisher_beat", new Dictionary<string, object>
            {
                ["battle_id"] = _context.BattleId,
                ["beat_index"] = (int)beat,
                ["landed"] = landed,
                ["offset_ms"] = offsetMs,
            });
        }

        void Finish()
        {
            if (!_running) return;
            _context.BeatsLandedInPhase3 = _sequencer.BeatsLanded;
            _running = false;
            _sequencer.BeatResolved -= OnBeatResolved;
            _onComplete();
        }
    }
}
