using UnityEngine;
using UnityEngine.UI;
using Kindrith.Core;
using Kindrith.Dialogue;

namespace Kindrith.UI
{
    public sealed class DialoguePanel : MonoBehaviour
    {
        const int OptionCount = 3;

        Canvas _canvas;
        Text _demonLineText;
        Button[] _optionButtons = new Button[OptionCount];
        Text[] _optionLabels = new Text[OptionCount];
        DialogueRunner _runner;

        public static DialoguePanel Create(Transform parent, Palette palette, DialogueRunner runner)
        {
            var panelGo = new GameObject("DialoguePanel",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            panelGo.transform.SetParent(parent, false);
            var canvas = panelGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            var scaler = panelGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgGo.transform.SetParent(panelGo.transform, false);
            var bgRt = (RectTransform)bgGo.transform;
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            bgGo.GetComponent<Image>().color = palette.Graphite;

            var demonGo = new GameObject("DemonLine",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            demonGo.transform.SetParent(panelGo.transform, false);
            var demonRt = (RectTransform)demonGo.transform;
            demonRt.anchorMin = new Vector2(0.1f, 0.55f);
            demonRt.anchorMax = new Vector2(0.9f, 0.85f);
            demonRt.sizeDelta = Vector2.zero;
            var demonText = demonGo.GetComponent<Text>();
            demonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            demonText.fontSize = 32;
            demonText.color = palette.Bone;
            demonText.alignment = TextAnchor.MiddleCenter;
            demonText.horizontalOverflow = HorizontalWrapMode.Wrap;

            var panel = panelGo.AddComponent<DialoguePanel>();
            panel._canvas = canvas;
            panel._demonLineText = demonText;
            panel._runner = runner;

            for (int i = 0; i < OptionCount; i++)
            {
                BuildOptionButton(panel, panelGo.transform, palette, i);
            }

            runner.NodeEntered += panel.OnNodeEntered;
            runner.Completed += panel.OnCompleted;
            panel.OnNodeEntered(runner.Current);
            return panel;
        }

        static void BuildOptionButton(DialoguePanel panel, Transform parent, Palette palette, int index)
        {
            var go = new GameObject($"Option{index}",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            float verticalSlot = 0.45f - index * 0.12f;
            rt.anchorMin = new Vector2(0.1f, verticalSlot - 0.05f);
            rt.anchorMax = new Vector2(0.9f, verticalSlot + 0.05f);
            rt.sizeDelta = Vector2.zero;
            go.GetComponent<Image>().color = palette.Duskwine;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = new Vector2(0.05f, 0f);
            labelRt.anchorMax = new Vector2(0.95f, 1f);
            labelRt.sizeDelta = Vector2.zero;
            var label = labelGo.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 24;
            label.color = palette.Bone;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;

            var button = go.GetComponent<Button>();
            int captured = index;
            button.onClick.AddListener(() =>
            {
                if (panel._runner != null && !panel._runner.IsComplete)
                    panel._runner.Choose(captured);
            });

            panel._optionButtons[index] = button;
            panel._optionLabels[index] = label;
        }

        void OnNodeEntered(DialogueNode node)
        {
            if (node == null) return;
            _demonLineText.text = node.DemonLine;

            int optionsAvailable = node.Options != null ? node.Options.Length : 0;
            for (int i = 0; i < OptionCount; i++)
            {
                bool active = i < optionsAvailable;
                _optionButtons[i].gameObject.SetActive(active);
                if (active)
                {
                    _optionLabels[i].text = node.Options[i].Text;
                }
            }
        }

        void OnCompleted()
        {
            for (int i = 0; i < OptionCount; i++)
            {
                if (_optionButtons[i] != null) _optionButtons[i].gameObject.SetActive(false);
            }
        }

        public void Dismiss()
        {
            if (_runner != null)
            {
                _runner.NodeEntered -= OnNodeEntered;
                _runner.Completed -= OnCompleted;
            }
            if (_canvas != null) Destroy(_canvas.gameObject);
        }
    }
}
