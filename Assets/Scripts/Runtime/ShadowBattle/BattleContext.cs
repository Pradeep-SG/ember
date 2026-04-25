using System;
using Kindrith.Dialogue;

namespace Kindrith.ShadowBattle
{
    public sealed class BattleContext
    {
        public BattleContext(ArchetypeId archetype, Beads demonBeads)
        {
            BattleId = Guid.NewGuid().ToString("N");
            Archetype = archetype;
            DemonBeads = demonBeads ?? throw new ArgumentNullException(nameof(demonBeads));
            Outcome = BattleOutcome.None;
        }

        // ULID-shaped identifier; WP-08 may swap in a real ULID generator alongside persistence.
        public string BattleId { get; }
        public ArchetypeId Archetype { get; }
        public Beads DemonBeads { get; }
        public int Phase1ElapsedMs { get; set; }
        public int CountersInPhase2 { get; set; }
        public int BeatsLandedInPhase3 { get; set; }
        public BattleOutcome Outcome { get; set; }
    }
}
