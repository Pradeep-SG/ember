using System.Collections.Generic;
using UnityEngine;
using Kindrith.Analytics;
using Kindrith.Dialogue;
using Kindrith.Persistence;

namespace Kindrith.UI
{
    public sealed class Boot : MonoBehaviour
    {
        [SerializeField] BattleRunner _battleRunner;

        AnalyticsBus _analytics;
        BattleStore _store;
        DefaultEnvelopeProvider _envelope;
        NdjsonAnalyticsSink _sink;
        HomeShell _homeShell;
        float _foregroundStartS;

        public AnalyticsBus Analytics => _analytics;
        public BattleStore Store => _store;
        public HomeShell HomeShell => _homeShell;
        public BattleRunner BattleRunner => _battleRunner;

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

            // Fall back to a scene lookup if the SerializeField wiring didn't survive a
            // scene reimport. The Bootstrap.unity YAML wires this directly, but if the
            // reference comes back null we still want a working build.
            if (_battleRunner == null)
            {
                _battleRunner = FindAnyObjectByType<BattleRunner>();
                if (_battleRunner == null)
                {
                    UnityEngine.Debug.LogWarning(
                        "Boot: BattleRunner not found in scene. Resist tap will emit shadow_battle_started but won't play a battle.");
                }
            }

            if (_battleRunner != null)
            {
                _battleRunner.Initialize(_analytics, _store);
                _battleRunner.BattleEnded += OnBattleEnded;
            }
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
            if (_battleRunner == null)
            {
                // No battle wiring — emit the trigger event so the analytics surface still
                // reflects the player's intent, then return to the home shell.
                _analytics.Emit("shadow_battle_started", new Dictionary<string, object>
                {
                    ["archetype"] = archetype.ToString().ToLowerInvariant(),
                    ["trigger"] = "user_resist_tap",
                });
                return;
            }

            _homeShell?.SetVisible(false);
            _battleRunner.StartBattle(archetype);
        }

        void OnBattleEnded()
        {
            _homeShell?.RefreshSessionLog();
            _homeShell?.SetVisible(true);
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
