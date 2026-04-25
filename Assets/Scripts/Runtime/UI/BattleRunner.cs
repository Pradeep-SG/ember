using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Kindrith.Analytics;
using Kindrith.Breathing;
using Kindrith.Core;
using Kindrith.Dialogue;
using Kindrith.Persistence;
using Kindrith.ShadowBattle;

namespace Kindrith.UI
{
    // Owns the Phase 1 → Phase 2 → Phase 3 → Outcome production playback. Composed by Boot
    // when the player taps Resist; tears itself down on RewardScreen advance and fires
    // BattleEnded so the home shell can refresh and re-show.
    public sealed class BattleRunner : MonoBehaviour
    {
        [SerializeField] Palette _palette;
        [SerializeField] DialogueTree _permissionGiverTree;
        [SerializeField] DialogueTree _tenderExcuseTree;
        [SerializeField] DialogueTree _tomorrowsWardenTree;

        AnalyticsBus _analytics;
        BattleStore _store;

        SystemClock _clock;
        BreathingClock _bclock;
        BattleStateMachine _sm;
        AnalyticsBusAdapter _emitter;

        Phase1Controller _phase1;
        Phase2Controller _phase2;
        Phase3Controller _phase3;
        DialogueRunner _dialogueRunner;
        FinisherSequencer _finisherSequencer;

        Canvas _phase1Canvas;
        BreathingCircleView _circleView;
        DialoguePanel _dialoguePanel;
        FinisherCueView _finisherCueView;
        RewardScreen _rewardScreen;

        DateTime _battleStartUtc;

        public event Action BattleEnded;

        public bool IsActive => _sm != null && _sm.Current != BattlePhase.Idle;

        public void Initialize(AnalyticsBus analytics, BattleStore store)
        {
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            if (_palette == null) _palette = ScriptableObject.CreateInstance<Palette>();
        }

        public void StartBattle(ArchetypeId archetype)
        {
            if (IsActive) return;
            _battleStartUtc = DateTime.UtcNow;
            _clock = new SystemClock();
            _bclock = new BreathingClock(_clock);
            _bclock.Start();

            _emitter = new AnalyticsBusAdapter(_analytics);
            _sm = new BattleStateMachine(_emitter);
            _sm.StartBattle(archetype);

            _analytics.Emit("shadow_battle_started", new Dictionary<string, object>
            {
                ["battle_id"] = _sm.Context.BattleId,
                ["archetype"] = archetype.ToString().ToLowerInvariant(),
                ["trigger"] = "user_resist_tap",
            });

            _phase1 = new Phase1Controller(
                _clock, _bclock, new TapEvaluator(), _sm.Context.DemonBeads, _sm.Context, _emitter, OnPhase1Complete);
            BuildPhase1Visual();
            _phase1.Start();
        }

        void Update()
        {
            if (_bclock != null) _bclock.Tick();
            if (_sm == null) return;

            switch (_sm.Current)
            {
                case BattlePhase.Phase1:
                    _phase1?.Tick();
                    if (TapPressedThisFrame()) _phase1?.RegisterTap();
                    break;
                case BattlePhase.Phase2:
                    _phase2?.Tick();
                    break;
                case BattlePhase.Phase3:
                    _phase3?.Tick();
                    if (TapPressedThisFrame()) _finisherCueView?.OnTapDown();
                    if (TapReleasedThisFrame()) _finisherCueView?.OnTapUp();
                    break;
            }
        }

        void BuildPhase1Visual()
        {
            var canvasGo = new GameObject("Phase1Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            _phase1Canvas = canvasGo.GetComponent<Canvas>();
            _phase1Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _phase1Canvas.sortingOrder = 30;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgGo.transform.SetParent(canvasGo.transform, false);
            var bgRt = (RectTransform)bgGo.transform;
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            bgGo.GetComponent<Image>().color = _palette.Graphite;

            var circleGo = new GameObject("BreathingCircle",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BreathingCircleView));
            circleGo.transform.SetParent(canvasGo.transform, false);
            var rt = (RectTransform)circleGo.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(420, 420);
            rt.anchoredPosition = Vector2.zero;

            var image = circleGo.GetComponent<Image>();
            image.sprite = CircleSpriteFactory.Create(256);
            image.color = _palette.Bone;

            _circleView = circleGo.GetComponent<BreathingCircleView>();
            _circleView.SetReferences(rt, image, _palette);
            _circleView.Bind(_bclock);
        }

        void OnPhase1Complete()
        {
            DismissPhase1();
            BuildPhase2();
        }

        void DismissPhase1()
        {
            if (_phase1Canvas != null) Destroy(_phase1Canvas.gameObject);
            _phase1Canvas = null;
            _circleView = null;
        }

        void BuildPhase2()
        {
            var tree = SelectTreeForArchetype(_sm.Context.Archetype);
            if (tree == null)
            {
                // No tree wired — auto-advance the state machine so the flow doesn't stall.
                _sm.Advance();
                BuildPhase3();
                return;
            }
            _dialogueRunner = new DialogueRunner(tree, _clock);
            _phase2 = new Phase2Controller(_dialogueRunner, _sm.Context, _emitter, OnPhase2Complete);
            _phase2.Start();
            _dialoguePanel = DialoguePanel.Create(transform, _palette, _dialogueRunner);
        }

        void OnPhase2Complete()
        {
            _dialoguePanel?.Dismiss();
            _dialoguePanel = null;
            BuildPhase3();
        }

        void BuildPhase3()
        {
            _finisherSequencer = new FinisherSequencer(_clock);
            _phase3 = new Phase3Controller(_clock, _finisherSequencer, _sm.Context, _emitter, OnPhase3Complete);
            _phase3.Start();
            _finisherCueView = FinisherCueView.Create(transform, _palette, _finisherSequencer, _clock);
            _finisherCueView.Begin();
        }

        void OnPhase3Complete()
        {
            _finisherCueView?.Dismiss();
            _finisherCueView = null;
            BuildRewardScreen();
        }

        void BuildRewardScreen()
        {
            var rewardGo = new GameObject("RewardScreen");
            rewardGo.transform.SetParent(transform, false);
            _rewardScreen = rewardGo.AddComponent<RewardScreen>();
            _rewardScreen.Show(_sm.Context, OnRewardAdvance);
        }

        void OnRewardAdvance()
        {
            try
            {
                _store.Save(BuildBattleRecord());
            }
            catch (Exception ex)
            {
                Log.Error($"BattleRunner: failed to save BattleRecord: {ex.Message}");
            }

            if (_sm != null && _sm.Current == BattlePhase.Outcome)
            {
                _sm.Advance(); // Outcome → Idle
            }

            Cleanup();
            BattleEnded?.Invoke();
        }

        BattleRecord BuildBattleRecord()
        {
            var endUtc = DateTime.UtcNow;
            int beadsExtinguished = 5 - (_sm.Context.DemonBeads != null ? _sm.Context.DemonBeads.Remaining : 5);

            return new BattleRecord
            {
                id = _sm.Context.BattleId,
                demon_archetype_used = _sm.Context.Archetype.ToString().ToLowerInvariant(),
                started_at = _battleStartUtc.ToString("o"),
                ended_at = endUtc.ToString("o"),
                total_duration_ms = (long)((endUtc - _battleStartUtc).TotalMilliseconds),
                trigger = "user_resist_tap",
                phase1 = new Phase1Record
                {
                    reached = true,
                    duration_ms = _sm.Context.Phase1ElapsedMs,
                    beads_extinguished = beadsExtinguished,
                },
                phase2 = new Phase2Record { reached = true },
                phase3 = new Phase3Record { reached = true, beats_landed = _sm.Context.BeatsLandedInPhase3 },
                outcome = _sm.Context.Outcome.ToString().ToLowerInvariant(),
                clarity_awarded = (_sm.Context.Outcome == BattleOutcome.Win || _sm.Context.Outcome == BattleOutcome.CriticalWin) ? 1 : 0,
                placeholder_reward_id = "shard_of_bone_placeholder",
            };
        }

        void Cleanup()
        {
            DismissPhase1();
            _dialoguePanel?.Dismiss();
            _finisherCueView?.Dismiss();
            _phase1 = null;
            _phase2 = null;
            _phase3 = null;
            _dialogueRunner = null;
            _finisherSequencer = null;
            _bclock = null;
            _sm = null;
        }

        DialogueTree SelectTreeForArchetype(ArchetypeId archetype)
        {
            switch (archetype)
            {
                case ArchetypeId.PermissionGiver: return _permissionGiverTree;
                case ArchetypeId.TenderExcuse: return _tenderExcuseTree;
                case ArchetypeId.TomorrowsWarden: return _tomorrowsWardenTree;
                default: return _permissionGiverTree;
            }
        }

        static bool TapPressedThisFrame()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
            return false;
        }

        static bool TapReleasedThisFrame()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame) return true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame) return true;
            return false;
        }
    }
}
