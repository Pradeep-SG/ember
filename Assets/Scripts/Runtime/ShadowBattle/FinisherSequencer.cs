using System;
using Kindrith.Core;

namespace Kindrith.ShadowBattle
{
    public sealed class FinisherSequencer
    {
        public const int BeatTapWindowMs = 180;
        public const int HoldDurationMs = 800;
        public const int HoldToleranceMs = 120;
        public const int Phase3MaxDurationMs = 30_000;

        // Default expected beat times relative to Start. Production scenes can calibrate these to
        // the visual cadence; tests pass them explicitly via the constructor.
        public const int DefaultExpandTapMs = 1_000;
        public const int DefaultContractTapMs = 2_500;
        public const int DefaultHoldStartMs = 4_000;

        readonly IClock _clock;
        readonly int _expandTapTargetMs;
        readonly int _contractTapTargetMs;
        readonly int _holdStartTargetMs;

        long _startMs;
        bool _started;
        bool _complete;
        int _beatsLanded;
        int _beatIndex;          // 0 → ExpandTap, 1 → ContractTap, 2 → HoldRelease
        long _holdStartMs;
        bool _holdStartRegistered;

        public FinisherSequencer(
            IClock clock,
            int expandTapTargetMs = DefaultExpandTapMs,
            int contractTapTargetMs = DefaultContractTapMs,
            int holdStartTargetMs = DefaultHoldStartMs)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _expandTapTargetMs = expandTapTargetMs;
            _contractTapTargetMs = contractTapTargetMs;
            _holdStartTargetMs = holdStartTargetMs;
        }

        public int BeatsLanded => _beatsLanded;
        public bool IsComplete => _complete;

        public event Action<FinisherBeat, bool, int> BeatResolved;

        public void Start()
        {
            _startMs = _clock.NowMs;
            _started = true;
        }

        public void Resume(int backgroundDurationMs)
        {
            if (!_started || _complete) return;
            _startMs += backgroundDurationMs;
        }

        public void Tick()
        {
            if (!_started || _complete) return;
            var elapsedMs = (int)(_clock.NowMs - _startMs);
            if (elapsedMs >= Phase3MaxDurationMs)
            {
                MissRemaining();
                _complete = true;
            }
        }

        public void RegisterTap(int atMs)
        {
            if (_complete || !_started) return;
            if (_beatIndex >= 2) return; // HoldRelease uses RegisterHoldStart / RegisterHoldRelease.

            int targetMs = _beatIndex == 0 ? _expandTapTargetMs : _contractTapTargetMs;
            int offset = atMs - targetMs;
            bool landed = Math.Abs(offset) <= BeatTapWindowMs;

            var beat = _beatIndex == 0 ? FinisherBeat.ExpandTap : FinisherBeat.ContractTap;
            ResolveBeat(beat, landed, offset);
            _beatIndex++;
        }

        public void RegisterHoldStart(int atMs)
        {
            if (_complete || !_started) return;
            if (_beatIndex != 2) return;
            _holdStartMs = atMs;
            _holdStartRegistered = true;
        }

        public void RegisterHoldRelease(int atMs)
        {
            if (_complete || !_started) return;
            if (_beatIndex != 2) return;
            if (!_holdStartRegistered) return;

            var holdDurationMs = (int)(atMs - _holdStartMs);
            int offset = holdDurationMs - HoldDurationMs;
            bool landed = Math.Abs(offset) <= HoldToleranceMs;

            ResolveBeat(FinisherBeat.HoldRelease, landed, offset);
            _beatIndex++;
            _complete = true;
        }

        void ResolveBeat(FinisherBeat beat, bool landed, int offsetMs)
        {
            if (landed) _beatsLanded++;
            BeatResolved?.Invoke(beat, landed, offsetMs);
        }

        void MissRemaining()
        {
            while (_beatIndex < 3)
            {
                ResolveBeat(BeatAtIndex(_beatIndex), landed: false, offsetMs: int.MaxValue);
                _beatIndex++;
            }
        }

        static FinisherBeat BeatAtIndex(int index)
        {
            switch (index)
            {
                case 0: return FinisherBeat.ExpandTap;
                case 1: return FinisherBeat.ContractTap;
                default: return FinisherBeat.HoldRelease;
            }
        }
    }
}
