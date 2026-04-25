using System.Collections.Generic;
using UnityEngine;
using Kindrith.Analytics;
using Kindrith.Dialogue;
using Kindrith.Persistence;

namespace Kindrith.UI
{
    // Bootstrap entry point. Initializes the analytics bus + battle store, builds the home
    // shell, and emits app_opened with resume_reason=cold_start. The full Phase 1 battle
    // playback (BreathingHarness → DialogueRunner UI → FinisherSequencer UI → RewardScreen)
    // is wired here as a separate Phase 1 polish PR; WP-08 ships the analytics/persistence
    // pipeline plus the home UI so the battle plug-in is straightforward.
    public sealed class Boot : MonoBehaviour
    {
        AnalyticsBus _analytics;
        BattleStore _store;
        DefaultEnvelopeProvider _envelope;
        NdjsonAnalyticsSink _sink;
        HomeShell _homeShell;
        float _foregroundStartS;

        public AnalyticsBus Analytics => _analytics;
        public BattleStore Store => _store;
        public HomeShell HomeShell => _homeShell;

        void Awake()
        {
            _envelope = new DefaultEnvelopeProvider();
            _sink = new NdjsonAnalyticsSink();
            _analytics = new AnalyticsBus(_sink, _envelope);
            _store = new BattleStore();
            _foregroundStartS = Time.realtimeSinceStartup;

            var homeGo = new GameObject("HomeShell");
            homeGo.transform.SetParent(transform, false);
            _homeShell = homeGo.AddComponent<HomeShell>();
            _homeShell.Initialize(_store);
            _homeShell.ResistRequested += OnResistRequested;
        }

        void Start()
        {
            _analytics.Emit("app_opened", new Dictionary<string, object>
            {
                ["resume_reason"] = "cold_start",
                ["time_since_last_open_ms"] = null,
            });
        }

        void OnResistRequested(ArchetypeId archetype)
        {
            // WP-08 surfaces the trigger event. Battle playback wiring lands in the Phase 1
            // polish PR — see docs/phase2-backlog.md for the deferral note.
            _analytics.Emit("shadow_battle_started", new Dictionary<string, object>
            {
                ["archetype"] = archetype.ToString().ToLowerInvariant(),
                ["trigger"] = "user_resist_tap",
            });
        }

        void OnApplicationPause(bool paused)
        {
            if (_analytics == null) return;
            if (paused)
            {
                var foregroundMs = (long)((Time.realtimeSinceStartup - _foregroundStartS) * 1000f);
                _analytics.Emit("app_backgrounded", new Dictionary<string, object>
                {
                    ["foreground_duration_ms"] = foregroundMs,
                    ["on_screen"] = "home",
                });
            }
            else
            {
                _foregroundStartS = Time.realtimeSinceStartup;
            }
        }
    }
}
