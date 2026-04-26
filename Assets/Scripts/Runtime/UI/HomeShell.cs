using System;
using UnityEngine;
using UnityEngine.UI;
using Kindrith.Core;
using Kindrith.Dialogue;
using Kindrith.Persistence;
using Kindrith.Resonance;

namespace Kindrith.UI
{
    // Phase 1 home shell. Self-builds a minimal canvas with a Hearth title, the Resist button,
    // a session log, and a dev-only archetype picker. Battle wiring (Phase 1/2/3 controllers,
    // RewardScreen) is intentionally NOT in this class — Boot constructs it on Resist tap so
    // HomeShell stays focused on UI shape and tests can drive the pipeline directly.
    public sealed class HomeShell : MonoBehaviour
    {
        [SerializeField] Palette _palette;

        BattleStore _store;
        ResonanceMeter _resonanceMeter;
        ResistButton _resistButton;
        SessionLogView _sessionLog;
        ArchetypePicker _archetypePicker;
        ResonanceMeterView _resonanceMeterView;
        Canvas _canvas;

        public event Action<ArchetypeId> ResistRequested;

        public ResistButton ResistButton => _resistButton;
        public SessionLogView SessionLog => _sessionLog;
        public ArchetypePicker ArchetypePicker => _archetypePicker;

        public void Initialize(BattleStore store, ResonanceMeter resonanceMeter = null)
        {
            if (_palette == null) _palette = ScriptableObject.CreateInstance<Palette>();
            _store = store;
            _resonanceMeter = resonanceMeter;
            BuildCanvas();
        }

        public void RefreshSessionLog() => _sessionLog?.Refresh();

        public void SetVisible(bool visible)
        {
            if (_canvas != null) _canvas.gameObject.SetActive(visible);
        }

        void BuildCanvas()
        {
            var canvasGo = new GameObject("HomeCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

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

            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            titleGo.transform.SetParent(canvasGo.transform, false);
            var titleRt = (RectTransform)titleGo.transform;
            titleRt.anchorMin = new Vector2(0.1f, 0.7f);
            titleRt.anchorMax = new Vector2(0.9f, 0.85f);
            titleRt.sizeDelta = Vector2.zero;
            var titleText = titleGo.GetComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 56;
            titleText.color = _palette.Hearth;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.text = Kindrith.Core.Strings.AppTitle;

            _resistButton = ResistButton.Create(canvasGo.transform, _palette);
            _resistButton.Clicked += OnResistClicked;

            if (_store != null)
            {
                _sessionLog = SessionLogView.Create(canvasGo.transform, _palette, _store);
            }

            _archetypePicker = ArchetypePicker.Create(canvasGo.transform, _palette);

            if (_resonanceMeter != null)
            {
                _resonanceMeterView = ResonanceMeterView.Create(canvasGo.transform, _palette, _resonanceMeter);
            }
        }

        void OnResistClicked()
        {
            var archetype = _archetypePicker != null ? _archetypePicker.Selected : ArchetypeId.PermissionGiver;
            UnityEngine.Debug.Log($"HomeShell.OnResistClicked: archetype={archetype}, listeners={ResistRequested?.GetInvocationList().Length ?? 0}");
            ResistRequested?.Invoke(archetype);
        }
    }
}
