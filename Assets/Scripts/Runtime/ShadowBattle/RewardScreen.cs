using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Kindrith.Core;

namespace Kindrith.ShadowBattle
{
    // Phase 1 placeholder reward screen. Renders the spec §5 / §6 copy in a single
    // full-screen overlay that auto-advances after AutoAdvanceSeconds or on tap.
    // Production reward UI lives in WP-08+ alongside the home shell.
    public sealed class RewardScreen : MonoBehaviour
    {
        // Spec §5 exact copy. Tests assert these match the spec — do not edit
        // without aligning docs/phase1-shadow-battle-spec.md.
        public const string CriticalWinHeader = "Critical Win.";
        public const string WinHeader = "Win.";
        public const string LossHeader = "The Demon is strong today.";
        public const string LossBody = "You showed up. That is the hardest part. Tomorrow you fight again.";
        public const string AbandonHeader = "Stepped back.";
        public const string AbandonBody = "The battle waits. Come back when you're ready.";

        public const string ClarityRewardLine = "Clarity +1";
        public const string ShardRewardLine = "Shard of Bone";
        public const string ReturnPrompt = "[Tap to return to the Realm]";

        public const float AutoAdvanceSeconds = 3f;

        [SerializeField] Palette _palette;

        Canvas _canvas;
        Action _onAdvance;
        float _showTimeS;
        bool _showing;

        public void Show(BattleContext context, Action onAdvance)
        {
            if (_palette == null) _palette = ScriptableObject.CreateInstance<Palette>();
            _onAdvance = onAdvance;
            _showTimeS = Time.unscaledTime;
            _showing = true;
            BuildCanvas(context);
        }

        void Update()
        {
            if (!_showing) return;
            if (Time.unscaledTime - _showTimeS >= AutoAdvanceSeconds) Advance();
        }

        void Advance()
        {
            if (!_showing) return;
            _showing = false;
            if (_canvas != null) Destroy(_canvas.gameObject);
            _canvas = null;
            var cb = _onAdvance;
            _onAdvance = null;
            cb?.Invoke();
        }

        void BuildCanvas(BattleContext context)
        {
            var canvasGo = new GameObject("RewardCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            // Full-screen tap target with the Graphite background.
            var bgGo = new GameObject("Background",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            bgGo.transform.SetParent(canvasGo.transform, false);
            var bgRt = (RectTransform)bgGo.transform;
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            bgGo.GetComponent<Image>().color = _palette.Graphite;
            bgGo.GetComponent<Button>().onClick.AddListener(Advance);

            var textGo = new GameObject("Body",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGo.transform.SetParent(canvasGo.transform, false);
            var textRt = (RectTransform)textGo.transform;
            textRt.anchorMin = new Vector2(0.1f, 0.1f);
            textRt.anchorMax = new Vector2(0.9f, 0.9f);
            textRt.sizeDelta = Vector2.zero;

            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 36;
            text.color = _palette.Bone;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = ComposeText(context);
        }

        static string ComposeText(BattleContext context)
        {
            var sb = new StringBuilder();
            sb.AppendLine(HeaderFor(context.Outcome));
            sb.AppendLine();

            switch (context.Outcome)
            {
                case BattleOutcome.CriticalWin:
                case BattleOutcome.Win:
                    sb.AppendLine(ClarityRewardLine);
                    sb.AppendLine(ShardRewardLine);
                    if (Debug.isDebugBuild)
                    {
                        sb.AppendLine();
                        sb.AppendLine(
                            $"[debug] phase1ms={context.Phase1ElapsedMs} " +
                            $"counters={context.CountersInPhase2} " +
                            $"beats={context.BeatsLandedInPhase3} " +
                            $"archetype={context.Archetype}");
                    }
                    break;
                case BattleOutcome.Loss:
                    sb.AppendLine(LossBody);
                    break;
                case BattleOutcome.Abandon:
                    sb.AppendLine(AbandonBody);
                    break;
            }

            sb.AppendLine();
            sb.AppendLine(ReturnPrompt);
            return sb.ToString();
        }

        static string HeaderFor(BattleOutcome outcome)
        {
            switch (outcome)
            {
                case BattleOutcome.CriticalWin: return CriticalWinHeader;
                case BattleOutcome.Win: return WinHeader;
                case BattleOutcome.Loss: return LossHeader;
                case BattleOutcome.Abandon: return AbandonHeader;
                default: return string.Empty;
            }
        }
    }
}
