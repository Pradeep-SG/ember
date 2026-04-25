using System;

namespace Kindrith.Data
{
    [Serializable]
    public sealed class StructureRecord
    {
        public string id;
        public int level;
        public bool dimmed;
        public bool locked;
    }
}
