using System;
using System.Collections.Generic;
using Kindrith.Core;
using Kindrith.Dialogue;

namespace Kindrith.ShadowBattle
{
    public sealed class Phase2Controller
    {
        readonly DialogueRunner _runner;
        readonly BattleContext _context;
        readonly IBattleEventEmitter _emitter;
        readonly IClock _clock;
        readonly Action _onComplete;

        bool _running;
        long _nodeEnteredMs;

        public Phase2Controller(
            DialogueRunner runner,
            BattleContext context,
            IBattleEventEmitter emitter,
            IClock clock,
            Action onComplete)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _emitter = emitter ?? throw new ArgumentNullException(nameof(emitter));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _onComplete = onComplete ?? throw new ArgumentNullException(nameof(onComplete));
        }

        public bool IsRunning => _running;
        public DialogueRunner Runner => _runner;

        public void Start()
        {
            if (_running) return;
            _running = true;
            _nodeEnteredMs = _clock.NowMs;
            _runner.NodeEntered += OnNodeEntered;
            _runner.OptionChosen += OnOptionChosen;
            _runner.Completed += OnCompleted;
        }

        void OnNodeEntered(DialogueNode _) => _nodeEnteredMs = _clock.NowMs;

        public void Tick()
        {
            if (!_running) return;
            _runner.Tick();
        }

        // Snapshot node id + index BEFORE Choose advances the runner — by the time
        // OptionChosen fires, _runner.Current may already be the next node.
        string _pendingNodeId;
        int _pendingOptionIndex;

        public void Choose(int optionIndex)
        {
            if (!_running) return;
            _pendingNodeId = _runner.Current?.Id;
            _pendingOptionIndex = optionIndex;
            _runner.Choose(optionIndex);
        }

        void OnOptionChosen(DialogueOption option, OptionClass cls)
        {
            if (cls == OptionClass.Counter) _context.CountersInPhase2++;

            var latencyMs = (int)Math.Max(0, _clock.NowMs - _nodeEnteredMs);
            _emitter.Emit("shadow_battle_dialogue_choice", new Dictionary<string, object>
            {
                ["battle_id"] = _context.BattleId,
                ["node_id"] = _pendingNodeId ?? _runner.Current?.Id,
                ["option_index"] = _pendingOptionIndex,
                ["option_class"] = cls.ToString().ToLowerInvariant(),
                ["latency_ms"] = latencyMs,
            });
        }

        void OnCompleted()
        {
            if (!_running) return;
            _running = false;
            _runner.NodeEntered -= OnNodeEntered;
            _runner.OptionChosen -= OnOptionChosen;
            _runner.Completed -= OnCompleted;
            _onComplete();
        }
    }
}
