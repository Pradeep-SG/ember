using System;
using Kindrith.Core;

namespace Kindrith.Breathing
{
    public sealed class BreathingClock
    {
        readonly IClock _clock;

        long _startMs;
        long _pausedAtMs;
        long _pausedOffsetMs;
        bool _running;
        int _lastFiredCycleStart;
        int _lastFiredExhale;

        public BreathingClock(IClock clock)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _lastFiredCycleStart = -1;
            _lastFiredExhale = -1;
        }

        public int CycleIndex { get; private set; }
        public int CycleElapsedMs { get; private set; }

        public BreathBeat CurrentBeat
        {
            get
            {
                int e = CycleElapsedMs;
                if (e < BreathingCadence.InhaleMs) return BreathBeat.Inhale;
                if (e < BreathingCadence.TapTargetMs) return BreathBeat.HoldTop;
                return BreathBeat.Exhale;
            }
        }

        public float CycleProgress01 => (float)CycleElapsedMs / BreathingCadence.CycleMs;

        public event Action<int> CycleStarted;
        public event Action<int> ExhaleStarted;

        public void Start()
        {
            _startMs = _clock.NowMs;
            _pausedOffsetMs = 0;
            _pausedAtMs = 0;
            _running = true;
            _lastFiredCycleStart = -1;
            _lastFiredExhale = -1;
            CycleIndex = 0;
            CycleElapsedMs = 0;
            Tick();
        }

        public void Pause()
        {
            if (!_running) return;
            _pausedAtMs = _clock.NowMs;
            _running = false;
        }

        public void Resume()
        {
            if (_running) return;
            _pausedOffsetMs += _clock.NowMs - _pausedAtMs;
            _running = true;
        }

        public void Tick()
        {
            if (!_running) return;

            long elapsedTotalMs = _clock.NowMs - _startMs - _pausedOffsetMs;
            if (elapsedTotalMs < 0) elapsedTotalMs = 0;

            int newCycleIndex = (int)(elapsedTotalMs / BreathingCadence.CycleMs);
            int cycleElapsedMs = (int)(elapsedTotalMs % BreathingCadence.CycleMs);

            while (_lastFiredCycleStart < newCycleIndex)
            {
                _lastFiredCycleStart++;
                CycleStarted?.Invoke(_lastFiredCycleStart);
            }

            // Past cycles whose exhale boundary we crossed without firing.
            while (_lastFiredExhale < newCycleIndex - 1)
            {
                _lastFiredExhale++;
                ExhaleStarted?.Invoke(_lastFiredExhale);
            }
            // Current cycle: fire once we've crossed the tap target.
            if (_lastFiredExhale < newCycleIndex && cycleElapsedMs >= BreathingCadence.TapTargetMs)
            {
                _lastFiredExhale = newCycleIndex;
                ExhaleStarted?.Invoke(newCycleIndex);
            }

            CycleIndex = newCycleIndex;
            CycleElapsedMs = cycleElapsedMs;
        }
    }
}
