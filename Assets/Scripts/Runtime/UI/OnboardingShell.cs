using System;
using UnityEngine;
using UnityEngine.UI;
using Kindrith.Core;
using Kindrith.Onboarding;

namespace Kindrith.UI
{
    // Programmatic onboarding UI. Self-builds a single overlay canvas; switches the
    // body panel based on OnboardingFlow.CurrentStep. Tutorial battle delegates to
    // an injected callback (Boot wires it to BattleRunner).
    public sealed class OnboardingShell : MonoBehaviour
    {
        [SerializeField] Palette _palette;

        OnboardingFlow _flow;
        Action _onTutorialBattleRequested;
        Action _onCompleted;

        Canvas _canvas;
        GameObject _body;

        // Default copy per WP-14 acceptance.
        public const string FirstOathTemplateTitle = "Read 20 minutes";
        public const string FirstOathTemplateWhy   = "Because the person I'm becoming reads.";
        public const string FirstChainTemplateTitle = "Scrolling after 10pm";

        public static OnboardingShell Create(Transform parent, Palette palette,
            OnboardingFlow flow, Action onTutorialBattleRequested, Action onCompleted)
        {
            if (palette == null) throw new ArgumentNullException(nameof(palette));
            if (flow == null) throw new ArgumentNullException(nameof(flow));

            var go = new GameObject("OnboardingShell");
            go.transform.SetParent(parent, false);
            var shell = go.AddComponent<OnboardingShell>();
            shell._palette = palette;
            shell._flow = flow;
            shell._onTutorialBattleRequested = onTutorialBattleRequested;
            shell._onCompleted = onCompleted;
            shell.BuildCanvas();
            shell.RenderCurrentStep();
            return shell;
        }

        public OnboardingFlow Flow => _flow;

        public void RenderCurrentStep()
        {
            if (_body != null) Destroy(_body);
            switch (_flow.CurrentStep)
            {
                case OnboardingStepId.WardenName: BuildWardenName(); break;
                case OnboardingStepId.ClassSelect: BuildClassSelect(); break;
                case OnboardingStepId.FirstOath: BuildFirstOath(); break;
                case OnboardingStepId.FirstChain: BuildFirstChain(); break;
                case OnboardingStepId.DemonSummon: BuildDemonSummon(); break;
                case OnboardingStepId.TutorialBattle: BuildTutorialBattle(); break;
                case OnboardingStepId.Complete: BuildComplete(); break;
            }
        }

        void BuildCanvas()
        {
            var canvasGo = new GameObject("OnboardingCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 80;

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
        }

        // -- Step builders -----------------------------------------------------

        void BuildWardenName()
        {
            _body = NewBodyPanel();
            BuildHeading("What should the realm call you?");
            var input = BuildTextInput("Your name", expectedLength: 24);
            BuildContinueButton("Begin", () =>
            {
                var name = (input.text ?? string.Empty).Trim();
                if (name.Length < 2 || name.Length > 24) return;
                Submit(OnboardingStepId.WardenName, new WardenNamePayload { DisplayName = name });
            });
        }

        void BuildClassSelect()
        {
            _body = NewBodyPanel();
            BuildHeading("Pick your path.");
            BuildClassRow("Scholar", "The one who studies the demon.", true,
                onSelect: () => Submit(OnboardingStepId.ClassSelect, new ClassSelectPayload { ClassId = "scholar" }),
                anchorY: 0.50f);
            BuildClassRow("Warrior", "Coming in v1.1", false, null, anchorY: 0.40f);
            BuildClassRow("Monk",    "Coming in v1.1", false, null, anchorY: 0.30f);
            BuildClassRow("Ranger",  "Coming in v1.1", false, null, anchorY: 0.20f);
        }

        void BuildFirstOath()
        {
            _body = NewBodyPanel();
            BuildHeading("Take your first Oath.");
            var title = BuildTextInput(FirstOathTemplateTitle, prefilled: FirstOathTemplateTitle);
            var why = BuildTextInput(FirstOathTemplateWhy, prefilled: FirstOathTemplateWhy, anchorY: 0.45f);
            BuildContinueButton("Take the Oath", () =>
            {
                Submit(OnboardingStepId.FirstOath, new FirstOathPayload
                {
                    Title = (title.text ?? string.Empty).Trim(),
                    Why = (why.text ?? string.Empty).Trim(),
                    ClassId = "scholar",
                });
            });
        }

        void BuildFirstChain()
        {
            _body = NewBodyPanel();
            BuildHeading("Name what you're stepping away from.");
            var title = BuildTextInput(FirstChainTemplateTitle, prefilled: FirstChainTemplateTitle);
            BuildContinueButton("Bind the Chain", () =>
            {
                Submit(OnboardingStepId.FirstChain, new FirstChainPayload
                {
                    Title = (title.text ?? string.Empty).Trim(),
                    SeverityHint = "moderate",
                });
            });
        }

        void BuildDemonSummon()
        {
            _body = NewBodyPanel();
            BuildHeading("A shadow rises.");
            BuildBodyText(
                "The Voidwalker — born of late-night scrolling. Soft-spoken, and very good at giving permission.\n\n" +
                "It will tell you that just one more won't matter. It will sound like a friend.\n\n" +
                "The work begins.",
                anchorY: 0.45f);
            BuildContinueButton("Face it", () =>
            {
                Submit(OnboardingStepId.DemonSummon, new DemonSummonPayload());
            });
        }

        void BuildTutorialBattle()
        {
            _body = NewBodyPanel();
            BuildHeading("Your first Shadow Battle.");
            BuildBodyText(
                "Three minutes. Breathe with the circle. Tap on the release.\n\n" +
                "The demon will speak. Counter when you can.",
                anchorY: 0.45f);
            BuildContinueButton("Begin the battle", () =>
            {
                _onTutorialBattleRequested?.Invoke();
            });
        }

        void BuildComplete()
        {
            _body = NewBodyPanel();
            BuildHeading("Welcome to your realm.");
            BuildBodyText(
                "You've taken an Oath. You've named a Chain. You've fought.\n\n" +
                "The realm is yours. Tend it well.",
                anchorY: 0.45f);
            BuildContinueButton("Enter", () => _onCompleted?.Invoke());
        }

        // -- UI helpers --------------------------------------------------------

        GameObject NewBodyPanel()
        {
            var panel = new GameObject("Body", typeof(RectTransform));
            panel.transform.SetParent(_canvas.transform, false);
            var rt = (RectTransform)panel.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            return panel;
        }

        void BuildHeading(string text)
        {
            var go = new GameObject("Heading",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(_body.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.1f, 0.75f);
            rt.anchorMax = new Vector2(0.9f, 0.86f);
            rt.sizeDelta = Vector2.zero;
            var t = go.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 48;
            t.color = _palette.Hearth;
            t.alignment = TextAnchor.MiddleCenter;
            t.text = text;
        }

        void BuildBodyText(string text, float anchorY = 0.55f)
        {
            var go = new GameObject("Body",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(_body.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.12f, anchorY - 0.12f);
            rt.anchorMax = new Vector2(0.88f, anchorY + 0.12f);
            rt.sizeDelta = Vector2.zero;
            var t = go.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 26;
            t.color = _palette.Bone;
            t.alignment = TextAnchor.UpperCenter;
            t.text = text;
        }

        InputField BuildTextInput(string placeholder, int expectedLength = 64,
            string prefilled = null, float anchorY = 0.6f)
        {
            var go = new GameObject(placeholder + "Input",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField));
            go.transform.SetParent(_body.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.15f, anchorY);
            rt.anchorMax = new Vector2(0.85f, anchorY + 0.07f);
            rt.sizeDelta = Vector2.zero;
            go.GetComponent<Image>().color = new Color(_palette.Bone.r, _palette.Bone.g, _palette.Bone.b, 0.15f);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var textRt = (RectTransform)textGo.transform;
            textRt.anchorMin = new Vector2(0.04f, 0f);
            textRt.anchorMax = new Vector2(0.96f, 1f);
            textRt.sizeDelta = Vector2.zero;
            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 28;
            text.color = _palette.Bone;
            text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = false;

            var ph = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            ph.transform.SetParent(go.transform, false);
            var phRt = (RectTransform)ph.transform;
            phRt.anchorMin = new Vector2(0.04f, 0f);
            phRt.anchorMax = new Vector2(0.96f, 1f);
            phRt.sizeDelta = Vector2.zero;
            var phText = ph.GetComponent<Text>();
            phText.font = text.font;
            phText.fontSize = 28;
            phText.color = new Color(_palette.Bone.r, _palette.Bone.g, _palette.Bone.b, 0.5f);
            phText.alignment = TextAnchor.MiddleLeft;
            phText.text = placeholder;

            var input = go.GetComponent<InputField>();
            input.targetGraphic = go.GetComponent<Image>();
            input.textComponent = text;
            input.placeholder = phText;
            input.characterLimit = expectedLength;
            if (!string.IsNullOrEmpty(prefilled)) input.text = prefilled;
            return input;
        }

        void BuildContinueButton(string label, Action onClick)
        {
            var btnGo = new GameObject(label + "Button",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(_body.transform, false);
            var rt = (RectTransform)btnGo.transform;
            rt.anchorMin = new Vector2(0.2f, 0.10f);
            rt.anchorMax = new Vector2(0.8f, 0.18f);
            rt.sizeDelta = Vector2.zero;
            btnGo.GetComponent<Image>().color = _palette.Hearth;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelGo.transform.SetParent(btnGo.transform, false);
            var labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.sizeDelta = Vector2.zero;
            var t = labelGo.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 32;
            t.color = _palette.Bone;
            t.alignment = TextAnchor.MiddleCenter;
            t.text = label;

            btnGo.GetComponent<Button>().onClick.AddListener(() => onClick?.Invoke());
        }

        void BuildClassRow(string title, string subtitle, bool selectable, Action onSelect, float anchorY)
        {
            var rowGo = new GameObject(title + "Row",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            rowGo.transform.SetParent(_body.transform, false);
            var rt = (RectTransform)rowGo.transform;
            rt.anchorMin = new Vector2(0.15f, anchorY);
            rt.anchorMax = new Vector2(0.85f, anchorY + 0.07f);
            rt.sizeDelta = Vector2.zero;
            var bg = rowGo.GetComponent<Image>();
            bg.color = selectable
                ? new Color(_palette.Hearth.r, _palette.Hearth.g, _palette.Hearth.b, 0.85f)
                : new Color(_palette.Bone.r, _palette.Bone.g, _palette.Bone.b, 0.10f);

            var labelGo = new GameObject("Label",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelGo.transform.SetParent(rowGo.transform, false);
            var labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.sizeDelta = Vector2.zero;
            var t = labelGo.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 24;
            t.color = _palette.Bone;
            t.alignment = TextAnchor.MiddleCenter;
            t.text = $"{title} — {subtitle}";

            var btn = rowGo.GetComponent<Button>();
            btn.interactable = selectable;
            if (selectable && onSelect != null) btn.onClick.AddListener(() => onSelect());
        }

        void Submit(OnboardingStepId step, object payload)
        {
            try
            {
                _flow.Submit(step, payload);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"OnboardingShell.Submit({step}): {ex.Message}");
                return;
            }
            RenderCurrentStep();
        }
    }
}
