using System;
using Kindrith.Breathing;

namespace Kindrith.ShadowBattle
{
    public sealed class TapEvaluator
    {
        readonly int _tapTargetMs;

        public TapEvaluator(int tapTargetMs = BreathingCadence.TapTargetMs)
        {
            _tapTargetMs = tapTargetMs;
        }

        public TapGrade Grade(int cycleElapsedMs, out int offsetMs)
        {
            offsetMs = cycleElapsedMs - _tapTargetMs;
            int abs = Math.Abs(offsetMs);
            if (abs <= TapWindows.PerfectMs) return TapGrade.Perfect;
            if (abs <= TapWindows.CleanMs) return TapGrade.Clean;
            if (abs <= TapWindows.LooseMs) return TapGrade.Loose;
            return TapGrade.Miss;
        }

        public static int DamageFor(TapGrade grade)
        {
            switch (grade)
            {
                case TapGrade.Perfect: return 2;
                case TapGrade.Clean: return 1;
                default: return 0;
            }
        }
    }
}
