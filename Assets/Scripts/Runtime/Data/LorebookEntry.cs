using System;

namespace Kindrith.Data
{
    // Stub — WP-18 finalizes the entry types and template fields.
    [Serializable]
    public sealed class LorebookEntry
    {
        public string id;
        public int schema_version = SchemaVersion.Lorebook;
        public string kind;
        public string text;
        public string created_at;
        public string updated_at;
    }
}
