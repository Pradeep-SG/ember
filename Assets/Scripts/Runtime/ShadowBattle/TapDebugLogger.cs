using UnityEngine;
using UnityEngine.InputSystem;
using Kindrith.Core;
using Kindrith.Breathing;

namespace Kindrith.ShadowBattle
{
    // Dev-only scaffold for the WP-03 ShadowBattle scene. Logs each tap against the
    // breathing clock state. Replaced by Phase1Controller in WP-04.
    [RequireComponent(typeof(BreathingHarness))]
    public sealed class TapDebugLogger : MonoBehaviour
    {
        BreathingHarness _harness;
        TapEvaluator _eval;

        void Awake()
        {
            _harness = GetComponent<BreathingHarness>();
            _eval = new TapEvaluator();
        }

        void Update()
        {
            if (!TapPressedThisFrame()) return;

            var clock = _harness.Clock;
            if (clock == null) return;

            var grade = _eval.Grade(clock.CycleElapsedMs, out int offsetMs);
            Log.Info($"tap @ cycle={clock.CycleIndex} elapsed={clock.CycleElapsedMs}ms grade={grade} offset={offsetMs}ms");
        }

        static bool TapPressedThisFrame()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
            return false;
        }
    }
}
