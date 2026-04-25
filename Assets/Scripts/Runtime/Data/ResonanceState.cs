using System;

namespace Kindrith.Data
{
    [Serializable]
    public sealed class ResonanceState
    {
        public int schema_version = SchemaVersion.Resonance;
        public float current_value;
        public string tier = "dim"; // dim | warm | bright | radiant
        public string last_updated_at;
        public string updated_at;
        public float decay_per_missed_day = 6.0f;
        public float gain_per_strong_day = 4.0f;
        public int sanctuary_days_remaining_this_week = 1;
        public int sanctuary_days_max_per_week = 1;
        public string last_sanctuary_used_at;
    }
}
