using System;

namespace Kindrith.Data
{
    [Serializable]
    public sealed class OathRecord
    {
        public string id;
        public int schema_version = SchemaVersion.Oath;
        public string title;
        public string why;
        public OathCadence cadence = new OathCadence();
        public string class_id;
        public string status = "active"; // active | paused | retired
        public string created_at;
        public string updated_at;
        public string paused_at;
        public string retired_at;
        public float resonance_contribution = 1.0f;
    }

    [Serializable]
    public sealed class OathCadence
    {
        public string kind = "daily"; // daily | weekly | custom
        public int per_week = 7;
        public string anchor_time_local;
    }
}
