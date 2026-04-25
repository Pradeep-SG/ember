using System;
using System.Collections.Generic;
using Kindrith.Dialogue;

namespace Kindrith.ShadowBattle
{
    public sealed class Phase2Controller
    {
        readonly DialogueRunner _runner;
        readonly BattleContext _context;
        readonly IBattleEventEmitter _emitter;
        readonly Action _onComplete;

        bool _running;

        public Phase2Controller(
            DialogueRunner runner,
            BattleContext context,
            IBattleEventEmitter emitter,
            Action onComplete)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _emitter = emitter ?? throw new ArgumentNullException(nameof(emitter));
            _onComplete = onComplete ?? throw new ArgumentNullException(nameof(onComplete));
        }

        public bool IsRunning => _running;
        public DialogueRunner Runner => _runner;

        public void Start()
        {
            if (_running) return;
            _running = true;
            _runner.OptionChosen += OnOptionChosen;
            _runner.Completed += OnCompleted;
        }

        public void Tick()
        {
            if (!_running) return;
            _runner.Tick();
        }

        public void Choose(int optionIndex)
        {
            if (!_running) return;
            _runner.Choose(optionIndex);
        }

        void OnOptionChosen(DialogueOption option, OptionClass cls)
        {
            if (cls == OptionClass.Counter) _context.CountersInPhase2++;

            _emitter.Emit("shadow_battle_dialogue_choice", new Dictionary<string, object>
            {
                ["node_id"] = _runner.Current?.Id,
                ["option_class"] = cls.ToString().ToLowerInvariant(),
                ["counters_so_far"] = _runner.CountersTaken,
            });
        }

        void OnCompleted()
        {
            if (!_running) return;
            _running = false;
            _runner.OptionChosen -= OnOptionChosen;
            _runner.Completed -= OnCompleted;
            _onComplete();
        }
    }
}
