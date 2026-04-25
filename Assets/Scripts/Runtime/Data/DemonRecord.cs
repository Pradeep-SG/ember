using System;

namespace Kindrith.Data
{
    [Serializable]
    public sealed class DemonRecord
    {
        public string id;
        public int schema_version = SchemaVersion.Demon;
        public string chain_id;
        public string archetype_primary;
        public string[] archetype_pool = Array.Empty<string>();
        public int hp_max = 100;
        public int hp_current = 100;
        public int damage_dealt_total;
        public RegenEvent[] regen_events = Array.Empty<RegenEvent>();
        public KillCriteria kill_criteria = new KillCriteria();
        public string state = "alive"; // alive | dying | slain
        public string created_at;
        public string updated_at;
        public string slain_at;
    }

    [Serializable]
    public sealed class RegenEvent
    {
        public string at;
        public int hp_restored;
        public string severity;
    }

    [Serializable]
    public sealed class KillCriteria
    {
        public int consecutive_days_at_zero_hp = 7;
    }
}
