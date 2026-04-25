using System;

namespace Kindrith.Data
{
    [Serializable]
    public sealed class HabitLogEntry
    {
        public string id;
        public int schema_version = SchemaVersion.HabitLog;
        public string kind; // oath_completed | chain_lapse
        public string oath_id; // present for oath_completed
        public string chain_id; // present for chain_lapse
        public string logged_at;
        public string updated_at;
        public HabitValue value = new HabitValue();
        public string severity; // chain_lapse: minor | moderate | major
        public string note;
        public HabitReward reward = new HabitReward();
    }

    [Serializable]
    public sealed class HabitValue
    {
        public int duration_minutes;
    }

    [Serializable]
    public sealed class HabitReward
    {
        public int xp;
        public string loot_drop_id;
        public string rarity;
    }
}
