using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using Kindrith.Analytics;
using Kindrith.Core;
using Kindrith.Dialogue;
using Kindrith.Persistence;
using Kindrith.Progression;
using Kindrith.Resonance;
using Kindrith.ShadowBattle;

namespace Kindrith.UI
{
    public sealed class Boot : MonoBehaviour
    {
        [SerializeField] BattleRunner _battleRunner;
        [SerializeField] ResonanceTuning _resonanceTuning;
        [SerializeField] ProgressionTuning _progressionTuning;

        AnalyticsBus _analytics;
        BattleStore _store;
        WardenStore _wardenStore;
        ResonanceStore _resonanceStore;
        HabitLogStore _habitLogStore;
        ChainStore _chainStore;
        ResonanceMeter _resonanceMeter;
        DailyResonanceTicker _resonanceTicker;
        Levels _levels;
        DefaultEnvelopeProvider _envelope;
        NdjsonAnalyticsSink _sink;
        HomeShell _homeShell;
        ResumeOrAbandonSheet _resumeSheet;
        SystemDayClock _dayClock;
        DateTime _lastAppOpenedUtc;

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
            _wardenStore = new WardenStore();
            _resonanceStore = new ResonanceStore();
            _habitLogStore = new HabitLogStore();
            _chainStore = new ChainStore();
            _dayClock = new SystemDayClock();
            _lastAppOpenedUtc = DateTime.UtcNow;

            if (_resonanceTuning == null)
            {
                _resonanceTuning = ScriptableObject.CreateInstance<ResonanceTuning>();
            }
            if (_progressionTuning == null)
            {
                _progressionTuning = ScriptableObject.CreateInstance<ProgressionTuning>();
            }
            _resonanceMeter = new ResonanceMeter(_resonanceStore, _resonanceTuning);
            _resonanceTicker = new DailyResonanceTicker(_resonanceMeter, _dayClock, _habitLogStore, _chainStore);
            _resonanceTicker.TickIfNeeded();

            // Levels uses an analytics-bus-shaped emitter so xp_granted /
            // class_evolution_triggered land in the same NDJSON sink as battle events.
            _levels = new Levels(_wardenStore, _progressionTuning, new AnalyticsBusAdapter(_analytics));

            EnsureEventSystem();

            var homeGo = new GameObject("HomeShell");
            homeGo.transform.SetParent(transform, false);
            _homeShell = homeGo.AddComponent<HomeShell>();
            _homeShell.Initialize(_store, _resonanceMeter, _levels);
            _homeShell.ResistRequested += OnResistRequested;
            _levels.LeveledUp += (from, to) => _homeShell?.RefreshXpBar();

            UnityEngine.Debug.Log($"Boot.Awake: _battleRunner SerializeField = {(_battleRunner == null ? "NULL" : _battleRunner.name)}");

            if (_battleRunner == null)
            {
                _battleRunner = FindAnyObjectByType<BattleRunner>();
                UnityEngine.Debug.Log($"Boot.Awake: FindAnyObjectByType<BattleRunner>() = {(_battleRunner == null ? "NULL" : _battleRunner.name)}");
            }

            if (_battleRunner != null)
            {
                _battleRunner.Initialize(_analytics, _store, _wardenStore, _levels, _progressionTuning);
                _battleRunner.BattleEnded += OnBattleEnded;
            }
            else
            {
                UnityEngine.Debug.LogWarning("Boot.Awake: BattleRunner not found — Resist will emit but not play a battle.");
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
            UnityEngine.Debug.Log($"Boot.OnResistRequested: archetype={archetype}, _battleRunner={(_battleRunner == null ? "NULL" : _battleRunner.name)}");

            if (_battleRunner == null)
            {
                UnityEngine.Debug.LogWarning("Boot.OnResistRequested: BattleRunner is null — skipping battle start.");
                return;
            }

            _homeShell?.SetVisible(false);
            UnityEngine.Debug.Log("Boot.OnResistRequested: calling BattleRunner.StartBattle");
            _battleRunner.StartBattle(archetype);
            UnityEngine.Debug.Log($"Boot.OnResistRequested: after StartBattle, IsActive={_battleRunner.IsActive}");
        }

        void OnBattleEnded()
        {
            _homeShell?.RefreshSessionLog();
            _homeShell?.SetVisible(true);
        }

        // Unity UI Buttons need an EventSystem to receive input; programmatic Canvas creation
        // doesn't auto-add one, and our hand-authored Bootstrap.unity scene doesn't include
        // one either. Spawn one if missing so the Resist tap actually fires.
        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
            DontDestroyOnLoad(go);
        }

        DateTime _backgroundedAtUtc;

        void OnApplicationPause(bool paused)
        {
            if (_analytics == null) return;
            if (paused)
            {
                _backgroundedAtUtc = DateTime.UtcNow;
                var foregroundMs = ComputeForegroundMs(_lastAppOpenedUtc, _backgroundedAtUtc);
                _analytics.Emit("app_backgrounded", new Dictionary<string, object>
                {
                    ["foreground_duration_ms"] = foregroundMs,
                    ["on_screen"] = _battleRunner != null && _battleRunner.IsActive ? "battle" : "home",
                });
            }
            else
            {
                int bgMs = 0;
                if (_backgroundedAtUtc != default)
                {
                    bgMs = (int)Math.Max(0, (DateTime.UtcNow - _backgroundedAtUtc).TotalMilliseconds);
                }

                if (_battleRunner != null && _battleRunner.IsActive)
                {
                    _battleRunner.HandleForegroundResume(bgMs, EnsureResumeSheet());
                }

                _lastAppOpenedUtc = DateTime.UtcNow;
                _analytics.Emit("app_opened", new Dictionary<string, object>
                {
                    ["resume_reason"] = "foreground_resume",
                    ["time_since_last_open_ms"] = (long)bgMs,
                });
                _resonanceTicker?.TickIfNeeded();
            }
        }

        // Public for unit testing — pin foreground_duration_ms to wall-clock UTC, not realtime.
        public static long ComputeForegroundMs(DateTime appOpenedUtc, DateTime backgroundedUtc)
        {
            var ms = (long)(backgroundedUtc - appOpenedUtc).TotalMilliseconds;
            return ms < 0 ? 0 : ms;
        }

        ResumeOrAbandonSheet EnsureResumeSheet()
        {
            if (_resumeSheet != null) return _resumeSheet;
            var go = new GameObject("ResumeOrAbandonSheet");
            go.transform.SetParent(transform, false);
            _resumeSheet = go.AddComponent<ResumeOrAbandonSheet>();
            return _resumeSheet;
        }
    }
}
