using UnityEngine;
using UnityEngine.UI;
using Kindrith.Core;
using Kindrith.ShadowBattle;

namespace Kindrith.UI
{
    // Phase 3 finisher visual. Sequential prompts driven by elapsed time:
    //   0–1000 ms     → "Get ready..." (ring grows)
    //   1000–1500 ms  → "TAP" expand-tap window
    //   1500–2500 ms  → ring shrinks
    //   2500–3000 ms  → "TAP" contract-tap window
    //   3000–4000 ms  → "Hold soon..."
    //   4000–4800 ms  → "HOLD" (ring fills)
    //   4800+ ms      → "RELEASE"
    // Input routing: BattleRunner forwards tap-down / tap-up via OnTapDown / OnTapUp.
    public sealed class FinisherCueView : MonoBehaviour
    {
        Canvas _canvas;
        Text _instructionText;
        RectTransform _ringRect;
        Image _ringImage;
        Palette _palette;
        FinisherSequencer _sequencer;
        IClock _clock;
        long _phaseStartMs;
        bool _started;
        bool _holdActive;

        public bool IsHoldActive => _holdActive;

        public static FinisherCueView Create(Transform parent, Palette palette, FinisherSequencer sequencer, IClock clock)
        {
            var canvasGo = new GameObject("FinisherCueCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(parent, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;

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
            bgGo.GetComponent<Image>().color = palette.Graphite;

            var ringGo = new GameObject("Ring", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            ringGo.transform.SetParent(canvasGo.transform, false);
            var ringRt = (RectTransform)ringGo.transform;
            ringRt.anchorMin = new Vector2(0.5f, 0.5f);
            ringRt.anchorMax = new Vector2(0.5f, 0.5f);
            ringRt.pivot = new Vector2(0.5f, 0.5f);
            ringRt.sizeDelta = new Vector2(420, 420);
            ringRt.anchoredPosition = Vector2.zero;
            var ringImage = ringGo.GetComponent<Image>();
            ringImage.color = palette.Bone;

            var instrGo = new GameObject("Instruction", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            instrGo.transform.SetParent(canvasGo.transform, false);
            var instrRt = (RectTransform)instrGo.transform;
            instrRt.anchorMin = new Vector2(0.1f, 0.78f);
            instrRt.anchorMax = new Vector2(0.9f, 0.92f);
            instrRt.sizeDelta = Vector2.zero;
            var instr = instrGo.GetComponent<Text>();
            instr.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            instr.fontSize = 56;
            instr.color = palette.Bone;
            instr.alignment = TextAnchor.MiddleCenter;

            var view = canvasGo.AddComponent<FinisherCueView>();
            view._canvas = canvas;
            view._palette = palette;
            view._sequencer = sequencer;
            view._clock = clock;
            view._instructionText = instr;
            view._ringRect = ringRt;
            view._ringImage = ringImage;
            return view;
        }

        public void Begin()
        {
            _phaseStartMs = _clock.NowMs;
            _started = true;
        }

        void Update()
        {
            if (!_started || _sequencer.IsComplete) return;
            long elapsedMs = _clock.NowMs - _phaseStartMs;
            ApplyVisual(elapsedMs);
        }

        void ApplyVisual(long elapsedMs)
        {
            float scale = 0.4f;
            string instr = "...";
            Color color = _palette.Bone;

            if (elapsedMs < 1_000)
            {
                instr = "Get ready...";
                float t = elapsedMs / 1_000f;
                scale = Mathf.Lerp(0.4f, 1f, t);
                color = _palette.Bone;
            }
            else if (elapsedMs < 1_500)
            {
                instr = "TAP";
                scale = 1f;
                color = _palette.Hearth;
            }
            else if (elapsedMs < 2_500)
            {
                instr = "...";
                float t = (elapsedMs - 1_500f) / 1_000f;
                scale = Mathf.Lerp(1f, 0.4f, t);
                color = _palette.Bone;
            }
            else if (elapsedMs < 3_000)
            {
                instr = "TAP";
                scale = 0.4f;
                color = _palette.Hearth;
            }
            else if (elapsedMs < 4_000)
            {
                instr = "Hold soon...";
                scale = 0.4f;
                color = _palette.Bone;
            }
            else if (elapsedMs < 4_800)
            {
                instr = _holdActive ? "...keep holding..." : "HOLD";
                float t = (elapsedMs - 4_000f) / 800f;
                scale = Mathf.Lerp(0.4f, 1f, t);
                color = _palette.Hearth;
            }
            else
            {
                instr = "RELEASE";
                scale = 1f;
                color = _palette.Hearth;
            }

            _ringRect.localScale = new Vector3(scale, scale, 1f);
            _ringImage.color = color;
            _instructionText.text = instr;
        }

        public void OnTapDown()
        {
            if (!_started || _sequencer.IsComplete) return;
            long elapsedMs = _clock.NowMs - _phaseStartMs;

            // Beats 0 and 1 (expand / contract taps) live before 4000 ms; the hold beat owns >= 4000 ms.
            if (_sequencer.BeatsLanded < 0 || elapsedMs < 4_000)
            {
                _sequencer.RegisterTap((int)elapsedMs);
            }
            else if (!_holdActive)
            {
                _sequencer.RegisterHoldStart((int)elapsedMs);
                _holdActive = true;
            }
        }

        public void OnTapUp()
        {
            if (!_started || _sequencer.IsComplete) return;
            if (_holdActive)
            {
                long elapsedMs = _clock.NowMs - _phaseStartMs;
                _sequencer.RegisterHoldRelease((int)elapsedMs);
                _holdActive = false;
            }
        }

        public void Dismiss()
        {
            if (_canvas != null) Destroy(_canvas.gameObject);
        }
    }
}
