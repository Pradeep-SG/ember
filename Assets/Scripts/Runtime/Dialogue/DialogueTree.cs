using UnityEngine;

namespace Kindrith.Dialogue
{
    [CreateAssetMenu(menuName = "Kindrith/Dialogue Tree", fileName = "DialogueTree")]
    public sealed class DialogueTree : ScriptableObject
    {
        public ArchetypeId Archetype;
        public string EntryNodeId;
        public DialogueNode[] Nodes;
    }
}
