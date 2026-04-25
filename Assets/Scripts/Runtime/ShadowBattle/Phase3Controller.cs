using System;

namespace Kindrith.ShadowBattle
{
    // Auto-advancing stub for WP-04. Real finisher sequencer lands in WP-06.
    public sealed class Phase3Controller
    {
        readonly Action _onComplete;
        bool _running;

        public Phase3Controller(Action onComplete)
        {
            _onComplete = onComplete ?? throw new ArgumentNullException(nameof(onComplete));
        }

        public bool IsRunning => _running;

        public void Start() { _running = true; }

        public void Tick()
        {
            if (!_running) return;
            _running = false;
            _onComplete();
        }
    }
}
