using System;

namespace Kindrith.Data
{
    [Serializable]
    public sealed class ChainRecord
    {
        public string id;
        public int schema_version = SchemaVersion.Chain;
        public string title;
        public string severity_hint = "moderate"; // mild | moderate | severe
        public string[] demon_archetype_ids = Array.Empty<string>();
        public string demon_id;
        public string status = "active"; // active | retired
        public string created_at;
        public string updated_at;
        public string retired_at;
        public string last_lapse_at;
    }
}
