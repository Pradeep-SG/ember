namespace Kindrith.Breathing
{
    public readonly struct BreathingCadence
    {
        public const int InhaleMs = 4_000;
        public const int HoldMs = 7_000;
        public const int ExhaleMs = 8_000;
        public const int CycleMs = InhaleMs + HoldMs + ExhaleMs;        // 19_000
        public const int TapTargetMs = InhaleMs + HoldMs;               // 11_000 — Hold→Exhale boundary
    }

    public enum BreathBeat
    {
        Inhale,
        HoldTop,
        Exhale,
    }
}
