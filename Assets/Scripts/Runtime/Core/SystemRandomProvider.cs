using System;

namespace Kindrith.Core
{
    public sealed class SystemRandomProvider : IRandomProvider
    {
        readonly Random _rng;

        public SystemRandomProvider() : this(Environment.TickCount) { }

        public SystemRandomProvider(int seed) { _rng = new Random(seed); }

        public int NextInt(int minInclusive, int maxExclusive) => _rng.Next(minInclusive, maxExclusive);

        public float NextFloat() => (float)_rng.NextDouble();

        public double NextDouble() => _rng.NextDouble();
    }
}
