using System;
using UnityEngine;
using UnityEngine.UI;
using Kindrith.Core;
using Kindrith.Progression;

namespace Kindrith.UI
{
    // Renders a "Lv N" label and a fill bar for XpInLevel / (XpInLevel + XpToNext).
    // Subscribes to LeveledUp; the parent (HomeShell) calls Refresh after GrantXp
    // so within-level progress also updates.
    public sealed class XpBarView : MonoBehaviour
    {
        [SerializeField] Palette _palette;

        Levels _levels;
        Image _fillImage;
        RectTransform _fillRt;
        Text _label;

        public static XpBarView Create(Transform parent, Palette palette, Levels levels)
        {
            if (palette == null) throw new ArgumentNullException(nameof(palette));
            if (levels == null) throw new ArgumentNullException(nameof(levels));

            var go = new GameObject("XpBar",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.1f, 0.91f);
            rt.anchorMax = new Vector2(0.9f, 0.94f);
            rt.sizeDelta = Vector2.zero;
            go.GetComponent<Image>().color = new Color(palette.Graphite.r, palette.Graphite.g, palette.Graphite.b, 0.6f);

            var fillGo = new GameObject("Fill",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillGo.transform.SetParent(go.transform, false);
            var fillRt = (RectTransform)fillGo.transform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;
            var fillImage = fillGo.GetComponent<Image>();
            fillImage.color = palette.Hearth;

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

            var view = go.AddComponent<XpBarView>();
            view._palette = palette;
            view._levels = levels;
            view._fillImage = fillImage;
            view._fillRt = fillRt;
            view._label = label;
            view.Refresh();
            levels.LeveledUp += view.OnLeveledUp;
            return view;
        }

        void OnDestroy()
        {
            if (_levels != null) _levels.LeveledUp -= OnLeveledUp;
        }

        void OnLeveledUp(int from, int to) => Refresh();

        public void Refresh()
        {
            if (_levels == null || _fillImage == null) return;
            int xpIn = _levels.XpInLevel;
            int xpToNext = _levels.XpToNext;
            int slice = xpIn + xpToNext;
            float pct = slice > 0 ? (float)xpIn / slice : 1f;
            _fillRt.anchorMax = new Vector2(Mathf.Clamp01(pct), 1f);
            _label.text = _levels.Level >= ProgressionTuning.MaxLevel
                ? $"Lv {_levels.Level} (Max)"
                : $"Lv {_levels.Level} — {xpIn}/{slice}";
        }
    }
}
