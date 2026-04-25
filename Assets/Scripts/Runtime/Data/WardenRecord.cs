using System;

namespace Kindrith.Data
{
    [Serializable]
    public sealed class WardenRecord
    {
        public string id;
        public int schema_version = SchemaVersion.Warden;
        public string display_name;
        public string class_id; // null until onboarding completes
        public string created_at;
        public string updated_at;
        public int level = 1;
        public int xp_total;
        public int xp_in_level;
        public CosmeticsEquipped cosmetics_equipped = new CosmeticsEquipped();
        public string[] cosmetics_owned = Array.Empty<string>();
        public string clarity_expires_at;
    }

    [Serializable]
    public sealed class CosmeticsEquipped
    {
        public string outfit_id;
        public string title_id;
        public string realm_biome_id;
    }
}
