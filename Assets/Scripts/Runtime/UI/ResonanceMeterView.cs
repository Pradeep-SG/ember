using System;
using UnityEngine;
using UnityEngine.UI;
using Kindrith.Core;
using Kindrith.Resonance;

namespace Kindrith.UI
{
    // Renders a thin Resonance bar with one of four tier colors. Mounts under the home
    // canvas above the title; subscribes to ResonanceMeter.TierChanged so the visual
    // refreshes on each transition. Phase 2 placeholder — Phase 3 swaps in the
    // animated bar from art-direction.md.
    public sealed class ResonanceMeterView : MonoBehaviour
    {
        [SerializeField] Palette _palette;

        ResonanceMeter _meter;
        Image _fillImage;
        RectTransform _fillRt;
        Text _label;

        public static ResonanceMeterView Create(Transform parent, Palette palette, ResonanceMeter meter)
        {
            if (palette == null) throw new ArgumentNullException(nameof(palette));
            if (meter == null) throw new ArgumentNullException(nameof(meter));

            var go = new GameObject("ResonanceMeter",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.1f, 0.86f);
            rt.anchorMax = new Vector2(0.9f, 0.9f);
            rt.sizeDelta = Vector2.zero;
            // Track background.
            go.GetComponent<Image>().color = new Color(palette.Graphite.r, palette.Graphite.g, palette.Graphite.b, 0.6f);

            var fillGo = new GameObject("Fill",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillGo.transform.SetParent(go.transform, false);
            var fillRt = (RectTransform)fillGo.transform;
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(1f, 1f);
            fillRt.sizeDelta = Vector2.zero;
            var fillImage = fillGo.GetComponent<Image>();

            var labelGo = new GameObject("Label",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.sizeDelta = Vector2.zero;
            var label = labelGo.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 18;
            label.color = palette.Bone;
            label.alignment = TextAnchor.MiddleCenter;

            var view = go.AddComponent<ResonanceMeterView>();
            view._palette = palette;
            view._meter = meter;
            view._fillImage = fillImage;
            view._fillRt = fillRt;
            view._label = label;
            view.Repaint();
            meter.TierChanged += view.OnTierChanged;
            return view;
        }

        void OnDestroy()
        {
            if (_meter != null) _meter.TierChanged -= OnTierChanged;
        }

        void OnTierChanged(ResonanceTier from, ResonanceTier to) => Repaint();

        void Repaint()
        {
            if (_meter == null || _fillImage == null) return;
            float pct = Mathf.Clamp01(_meter.CurrentValue / 100f);
            _fillRt.anchorMax = new Vector2(pct, 1f);
            _fillImage.color = ColorForTier(_meter.CurrentTier);
            _label.text = $"Resonance: {_meter.CurrentTier}";
        }

        Color ColorForTier(ResonanceTier tier)
        {
            switch (tier)
            {
                case ResonanceTier.Dim: return new Color(_palette.Bone.r * 0.5f, _palette.Bone.g * 0.5f, _palette.Bone.b * 0.5f, 0.6f);
                case ResonanceTier.Warm: return _palette.Hearth;
                case ResonanceTier.Bright: return new Color(_palette.Hearth.r * 1.1f, _palette.Hearth.g * 1.1f, _palette.Hearth.b * 0.8f);
                case ResonanceTier.Radiant: return _palette.Bone;
                default: return _palette.Bone;
            }
        }
    }
}
