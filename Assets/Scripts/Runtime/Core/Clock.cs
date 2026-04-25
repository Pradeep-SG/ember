using UnityEngine;

namespace Kindrith.Core
{
    public interface IClock
    {
        long NowMs { get; }
    }

    public sealed class SystemClock : IClock
    {
        public long NowMs => (long)(Time.unscaledTimeAsDouble * 1000.0);
    }

    public sealed class FakeClock : IClock
    {
        long _nowMs;

        public FakeClock(long startMs = 0) { _nowMs = startMs; }

        public long NowMs => _nowMs;

        public void SetNowMs(long ms) { _nowMs = ms; }

        public void Advance(long deltaMs) { _nowMs += deltaMs; }
    }
}
