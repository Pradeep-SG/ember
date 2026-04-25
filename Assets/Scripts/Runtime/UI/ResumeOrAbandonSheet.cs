using System;
using UnityEngine;
using UnityEngine.UI;
using Kindrith.Core;

namespace Kindrith.UI
{
    // Sub-60s background-resume sheet. Spec §2.4 / WP-04 deferred → WP-10.
    // Show() builds an overlay canvas with header, body, Resume + Abandon buttons.
    // Hide() destroys the overlay. ResumeChosen / AbandonChosen events fire on tap.
    public sealed class ResumeOrAbandonSheet : MonoBehaviour
    {
        public event Action ResumeChosen;
        public event Action AbandonChosen;

        [SerializeField] Palette _palette;

        Canvas _canvas;
        int _backgroundDurationMs;

        public bool IsShowing => _canvas != null;
        public int LastShownBackgroundDurationMs => _backgroundDurationMs;

        public void Show(int backgroundDurationMs)
        {
            if (_canvas != null) Hide();
            _backgroundDurationMs = backgroundDurationMs;
            if (_palette == null) _palette = ScriptableObject.CreateInstance<Palette>();
            BuildCanvas();
        }

        public void Hide()
        {
            if (_canvas == null) return;
            Destroy(_canvas.gameObject);
            _canvas = null;
        }

        void BuildCanvas()
        {
            var canvasGo = new GameObject("ResumeOrAbandonCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 110; // above RewardCanvas (100) and AbandonCanvas (90)

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            var bgGo = new GameObject("Background",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgGo.transform.SetParent(canvasGo.transform, false);
            var bgRt = (RectTransform)bgGo.transform;
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            var bgColor = _palette.Graphite;
            bgColor.a = 0.92f;
            bgGo.GetComponent<Image>().color = bgColor;

            BuildText("Header", new Vector2(0.1f, 0.62f), new Vector2(0.9f, 0.78f),
                Strings.ResumeSheetHeader, 56, _palette.Hearth, canvasGo.transform);
            BuildText("Body", new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.6f),
                Strings.ResumeSheetBody, 28, _palette.Bone, canvasGo.transform);

            BuildButton(canvasGo.transform, new Vector2(0.15f, 0.28f), new Vector2(0.48f, 0.4f),
                Strings.ResumeSheetResume, _palette.Hearth, FireResume);
            BuildButton(canvasGo.transform, new Vector2(0.52f, 0.28f), new Vector2(0.85f, 0.4f),
                Strings.ResumeSheetAbandon, _palette.Duskwine, FireAbandon);
        }

        void FireResume()
        {
            ResumeChosen?.Invoke();
            Hide();
        }

        void FireAbandon()
        {
            AbandonChosen?.Invoke();
            Hide();
        }

        void BuildText(string name, Vector2 anchorMin, Vector2 anchorMax, string text, int fontSize, Color color, Transform parent)
        {
            var go = new GameObject(name,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = Vector2.zero;
            var t = go.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.text = text;
        }

        void BuildButton(Transform parent, Vector2 anchorMin, Vector2 anchorMax, string label, Color bgColor, Action onClick)
        {
            var btnGo = new GameObject(label + "Button",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);
            var rt = (RectTransform)btnGo.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = Vector2.zero;
            btnGo.GetComponent<Image>().color = bgColor;

            var labelGo = new GameObject("Label",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelGo.transform.SetParent(btnGo.transform, false);
            var labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.sizeDelta = Vector2.zero;
            var lt = labelGo.GetComponent<Text>();
            lt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lt.fontSize = 36;
            lt.color = _palette.Bone;
            lt.alignment = TextAnchor.MiddleCenter;
            lt.text = label;

            btnGo.GetComponent<Button>().onClick.AddListener(() => onClick());
        }
    }
}
