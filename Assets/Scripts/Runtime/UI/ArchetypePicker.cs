using System;
using UnityEngine;
using UnityEngine.UI;
using Kindrith.Core;
using Kindrith.Dialogue;

namespace Kindrith.UI
{
    // DEV-only. Visible when Application.isEditor || Debug.isDebugBuild. Production builds skip
    // creation entirely so the user cannot reach this picker.
    public sealed class ArchetypePicker : MonoBehaviour
    {
        public event Action<ArchetypeId> Picked;

        public ArchetypeId Selected { get; private set; } = ArchetypeId.PermissionGiver;

        public static ArchetypePicker Create(Transform parent, Palette palette)
        {
            if (!Application.isEditor && !Debug.isDebugBuild) return null;

            var go = new GameObject("ArchetypePicker",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.65f, 0.85f);
            rt.anchorMax = new Vector2(0.95f, 0.98f);
            rt.sizeDelta = Vector2.zero;
            go.GetComponent<Image>().color = new Color(palette.Duskwine.r, palette.Duskwine.g, palette.Duskwine.b, 0.7f);
            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 4;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var picker = go.AddComponent<ArchetypePicker>();
            foreach (var archetype in new[] { ArchetypeId.PermissionGiver, ArchetypeId.TenderExcuse, ArchetypeId.TomorrowsWarden })
            {
                picker.AddArchetypeButton(go.transform, palette, archetype);
            }
            return picker;
        }

        void AddArchetypeButton(Transform parent, Palette palette, ArchetypeId archetype)
        {
            var btnGo = new GameObject($"Pick_{archetype}",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            btnGo.transform.SetParent(parent, false);
            btnGo.GetComponent<Image>().color = palette.Graphite;
            btnGo.GetComponent<LayoutElement>().preferredHeight = 36;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelGo.transform.SetParent(btnGo.transform, false);
            var labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.sizeDelta = Vector2.zero;

            var label = labelGo.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 18;
            label.color = palette.Bone;
            label.alignment = TextAnchor.MiddleCenter;
            label.text = archetype.ToString();

            btnGo.GetComponent<Button>().onClick.AddListener(() =>
            {
                Selected = archetype;
                Picked?.Invoke(archetype);
            });
        }
    }
}
