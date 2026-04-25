using System;

namespace Kindrith.Data
{
    [Serializable]
    public sealed class RealmState
    {
        public int schema_version = SchemaVersion.Realm;
        public string biome_id = "forest_default";
        public StructureRecord[] structures = Array.Empty<StructureRecord>();
        public float shadow_territory_pct;
        public string last_tended_at;
        public string updated_at;
    }
}
