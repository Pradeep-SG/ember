namespace Kindrith.Loot
{
    public readonly struct LootDrop
    {
        public readonly string CatalogId;
        public readonly Rarity Rarity;
        public readonly bool IsFunctional; // false = cosmetic
        public readonly string Source;     // "shadow_battle_win" / "oath_completion" / etc.

        public LootDrop(string catalogId, Rarity rarity, bool isFunctional, string source)
        {
            CatalogId = catalogId;
            Rarity = rarity;
            IsFunctional = isFunctional;
            Source = source;
        }

        public static LootDrop None => default;
        public bool IsNone => string.IsNullOrEmpty(CatalogId);
    }
}
