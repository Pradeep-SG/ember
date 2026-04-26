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

        // Phase 2 Counter-option side-effect: each Counter pumps +0.1 into this pool,
        // clamped to [0, 1]. Phase 3 reads it; spec §5.1 turns full Clarity into the
        // 24h Warden Clarity buff on Win.
        public float ClarityPool { get; set; }
    }
}
