using System;

namespace Kindrith.ShadowBattle
{
    public sealed class Beads
    {
        public Beads(int max = 5)
        {
            Max = max;
            Remaining = max;
        }

        public int Max { get; }
        public int Remaining { get; private set; }
        public bool Extinguished => Remaining == 0;

        public event Action<int> Changed;

        public void Damage(int amount)
        {
            if (amount <= 0) return;
            int next = Math.Max(0, Remaining - amount);
            if (next == Remaining) return;
            Remaining = next;
            Changed?.Invoke(Remaining);
        }

        // Phase 2 Agree-option side-effect: Demon partially regenerates. Clamped to Max.
        public void Regen(int amount)
        {
            if (amount <= 0) return;
            int next = Math.Min(Max, Remaining + amount);
            if (next == Remaining) return;
            Remaining = next;
            Changed?.Invoke(Remaining);
        }
    }
}
