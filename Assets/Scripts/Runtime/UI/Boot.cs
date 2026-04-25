using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
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
        ResumeOrAbandonSheet _resumeSheet;
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
            _lastAppOpenedUtc = DateTime.UtcNow;

            EnsureEventSystem();

            var homeGo = new GameObject("HomeShell");
            homeGo.transform.SetParent(transform, false);
            _homeShell = homeGo.AddComponent<HomeShell>();
            _homeShell.Initialize(_store);
            _homeShell.ResistRequested += OnResistRequested;

            UnityEngine.Debug.Log($"Boot.Awake: _battleRunner SerializeField = {(_battleRunner == null ? "NULL" : _battleRunner.name)}");

            if (_battleRunner == null)
            {
                _battleRunner = FindAnyObjectByType<BattleRunner>();
                UnityEngine.Debug.Log($"Boot.Awake: FindAnyObjectByType<BattleRunner>() = {(_battleRunner == null ? "NULL" : _battleRunner.name)}");
            }

            if (_battleRunner != null)
            {
                _battleRunner.Initialize(_analytics, _store);
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
