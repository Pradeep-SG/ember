using System;

namespace Kindrith.Persistence
{
    // Phase 1 subset of docs/data-model.md §5 — chain_id and demon_id are Phase 2 concepts
    // and intentionally omitted. Field names are snake_case to match the on-disk JSON schema.
    [Serializable]
    public sealed class BattleRecord
    {
        public string id;
        public int schema_version = 1;
        public string demon_archetype_used;
        public string started_at;
        public string ended_at;
        public long total_duration_ms;
        public string trigger;
        public Phase1Record phase1 = new Phase1Record();
        public Phase2Record phase2 = new Phase2Record();
        public Phase3Record phase3 = new Phase3Record();
        public string outcome;
        public int clarity_awarded;
        public string placeholder_reward_id;
    }

    [Serializable]
    public sealed class Phase1Record
    {
        public bool reached;
        public long duration_ms;
        public TapEntry[] taps = Array.Empty<TapEntry>();
        public int beads_extinguished;
    }

    [Serializable]
    public sealed class TapEntry
    {
        public int cycle_index;
        public int offset_ms;
        public string grade;
    }

    [Serializable]
    public sealed class Phase2Record
    {
        public bool reached;
        public long duration_ms;
        public DialogueTurn[] turns = Array.Empty<DialogueTurn>();
    }

    [Serializable]
    public sealed class DialogueTurn
    {
        public string node_id;
        public string option_class;
        public long latency_ms;
    }

    [Serializable]
    public sealed class Phase3Record
    {
        public bool reached;
        public long duration_ms;
        public int beats_landed;
    }
}
