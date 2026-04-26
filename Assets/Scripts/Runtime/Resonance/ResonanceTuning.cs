using UnityEngine;

namespace Kindrith.Resonance
{
    [CreateAssetMenu(menuName = "Kindrith/Resonance Tuning", fileName = "ResonanceTuning")]
    public sealed class ResonanceTuning : ScriptableObject
    {
        public float DecayPerMissedDay = 6f;
        public float GainPerStrongDay = 4f;
        public float DimMaxValue = 25f;
        public float WarmMaxValue = 60f;
        public float BrightMaxValue = 85f;
        public int SanctuaryDaysMaxPerWeek = 1;

        public ResonanceTier TierFor(float value)
        {
            if (value <= DimMaxValue) return ResonanceTier.Dim;
            if (value <= WarmMaxValue) return ResonanceTier.Warm;
            if (value <= BrightMaxValue) return ResonanceTier.Bright;
            return ResonanceTier.Radiant;
        }
    }
}
