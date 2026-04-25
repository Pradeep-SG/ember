using System;

namespace Kindrith.Dialogue
{
    [Serializable]
    public sealed class DialogueOption
    {
        public string Text;
        public OptionClass Class;
        public string LeadsToNodeId;
    }
}
