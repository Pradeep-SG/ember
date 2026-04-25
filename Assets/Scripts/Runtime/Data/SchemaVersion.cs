namespace Kindrith.Data
{
    // Per-entity schema version constants. Bump in lockstep with edits to docs/data-model.md
    // and add a new Migrations/M_NNNN class.
    public static class SchemaVersion
    {
        public const int BattleRecord = 1;
        public const int Warden = 1;
        public const int Oath = 1;
        public const int Chain = 1;
        public const int Demon = 1;
        public const int Resonance = 1;
        public const int HabitLog = 1;
        public const int Realm = 1;
        public const int Lorebook = 1;
    }
}
