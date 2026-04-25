using System;
using UnityEngine;
using UnityEngine.UI;
using Kindrith.Core;

namespace Kindrith.UI
{
    public sealed class ResistButton : MonoBehaviour
    {
        public event Action Clicked;

        public static ResistButton Create(Transform parent, Palette palette)
        {
            var go = new GameObject("ResistButton",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.4f);
            rt.anchorMax = new Vector2(0.5f, 0.4f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(560, 140);
            rt.anchoredPosition = Vector2.zero;
            go.GetComponent<Image>().color = palette.Hearth;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.sizeDelta = Vector2.zero;
            var label = labelGo.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 48;
            label.color = palette.Bone;
            label.alignment = TextAnchor.MiddleCenter;
            label.text = Kindrith.Core.Strings.ResistButton;

            var component = go.AddComponent<ResistButton>();
            go.GetComponent<Button>().onClick.AddListener(component.Fire);
            return component;
        }

        void Fire()
        {
            Clicked?.Invoke();
        }
    }
}
