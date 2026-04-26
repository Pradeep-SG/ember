using Kindrith.Data;

namespace Kindrith.Progression
{
    // Phase 2 ships a single class evolution at Lv 10: the Scholar visual swap.
    // The brief reserves cosmetic id "scholar_evolved_v1" — the Phase 2 hearth view
    // (WP-17) reads it from WardenRecord.cosmetics_equipped.outfit_id.
    public static class ScholarEvolution
    {
        public const string EvolvedOutfitId = "scholar_evolved_v1";

        public static bool ShouldEvolve(int newLevel) => newLevel == 10;

        public static void Apply(WardenRecord warden)
        {
            if (warden == null) return;
            if (warden.cosmetics_equipped == null)
            {
                warden.cosmetics_equipped = new CosmeticsEquipped();
            }
            warden.cosmetics_equipped.outfit_id = EvolvedOutfitId;
        }
    }
}
