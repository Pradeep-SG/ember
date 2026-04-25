using System;

namespace Kindrith.Dialogue
{
    [Serializable]
    public sealed class DialogueNode
    {
        public string Id;
        public string DemonLine;
        public DialogueOption[] Options;
        // For transitional nodes (Options is empty), points at the node to auto-advance to.
        public string NextNodeId;
    }
}
