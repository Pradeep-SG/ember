using System;
using Kindrith.Core;

namespace Kindrith.Dialogue
{
    public sealed class DialogueRunner
    {
        public const int OptionTimeoutMs = 20_000;
        public const int RequiredChoices = 2;

        readonly DialogueTree _tree;
        readonly IClock _clock;

        DialogueNode _current;
        long _nodeEnteredMs;
        int _choicesMade;
        int _countersTaken;
        bool _complete;

        public DialogueRunner(DialogueTree tree, IClock clock)
        {
            _tree = tree ?? throw new ArgumentNullException(nameof(tree));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            EnterNode(FindNode(tree.EntryNodeId));
        }

        public DialogueNode Current => _current;
        public bool IsComplete => _complete;
        public int CountersTaken => _countersTaken;

        public event Action<DialogueNode> NodeEntered;
        public event Action<DialogueOption, OptionClass> OptionChosen;
        public event Action Completed;

        // Re-arm after a background pause so elapsed-since-EnterNode excludes the bg duration.
        public void Resume(int backgroundDurationMs)
        {
            if (_complete) return;
            _nodeEnteredMs += backgroundDurationMs;
        }

        public void Tick()
        {
            if (_complete || _current == null) return;

            // Auto-advance transitional nodes (Options empty + NextNodeId set).
            if (HasNoOptions(_current))
            {
                if (!string.IsNullOrEmpty(_current.NextNodeId))
                {
                    AdvanceTo(_current.NextNodeId);
                }
                else
                {
                    // Terminal with no auto-advance and no options — complete.
                    CompleteRun();
                }
                return;
            }

            // Choice node: 20s soft timeout → auto-Deflect.
            long elapsed = _clock.NowMs - _nodeEnteredMs;
            if (elapsed >= OptionTimeoutMs)
            {
                int idx = FindFirstDeflectIndex(_current.Options);
                Choose(idx);
            }
        }

        public void Choose(int optionIndex)
        {
            if (_complete || _current == null) return;
            if (_current.Options == null || _current.Options.Length == 0)
                throw new InvalidOperationException("Cannot Choose at a node with no options");
            if (optionIndex < 0 || optionIndex >= _current.Options.Length)
                throw new ArgumentOutOfRangeException(nameof(optionIndex));

            var option = _current.Options[optionIndex];
            if (option.Class == OptionClass.Counter) _countersTaken++;
            _choicesMade++;
            OptionChosen?.Invoke(option, option.Class);

            if (!string.IsNullOrEmpty(option.LeadsToNodeId))
            {
                AdvanceTo(option.LeadsToNodeId);
            }

            if (_choicesMade >= RequiredChoices)
            {
                CompleteRun();
            }
        }

        void AdvanceTo(string nodeId)
        {
            var node = FindNode(nodeId);
            if (node == null)
            {
                CompleteRun();
                return;
            }
            EnterNode(node);
        }

        void EnterNode(DialogueNode node)
        {
            _current = node;
            _nodeEnteredMs = _clock.NowMs;
            NodeEntered?.Invoke(node);
        }

        void CompleteRun()
        {
            if (_complete) return;
            _complete = true;
            Completed?.Invoke();
        }

        DialogueNode FindNode(string id)
        {
            if (string.IsNullOrEmpty(id) || _tree.Nodes == null) return null;
            foreach (var node in _tree.Nodes)
            {
                if (node != null && node.Id == id) return node;
            }
            return null;
        }

        static bool HasNoOptions(DialogueNode node) => node.Options == null || node.Options.Length == 0;

        static int FindFirstDeflectIndex(DialogueOption[] options)
        {
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i].Class == OptionClass.Deflect) return i;
            }
            return 0;
        }
    }
}
