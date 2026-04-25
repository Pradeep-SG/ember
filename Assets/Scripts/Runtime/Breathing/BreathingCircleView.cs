using UnityEngine;
using UnityEngine.UI;
using Kindrith.Core;

namespace Kindrith.Breathing
{
    public sealed class BreathingCircleView : MonoBehaviour
    {
        const float MinScale = 0.40f;
        const float MaxScale = 1.00f;

        [SerializeField] RectTransform _circleRect;
        [SerializeField] Image _circleImage;
        [SerializeField] Palette _palette;

        BreathingClock _clock;

        public void Bind(BreathingClock clock) { _clock = clock; }

        public void SetReferences(RectTransform circleRect, Image circleImage, Palette palette)
        {
            _circleRect = circleRect;
            _circleImage = circleImage;
            _palette = palette;
        }

        void LateUpdate()
        {
            if (_clock == null || _palette == null || _circleRect == null || _circleImage == null) return;

            float scale = ScaleAt(_clock.CurrentBeat, _clock.CycleElapsedMs);
            _circleRect.localScale = new Vector3(scale, scale, 1f);
            _circleImage.color = ColorAt(_clock.CurrentBeat, _clock.CycleElapsedMs, _palette);
        }

        static float ScaleAt(BreathBeat beat, int cycleElapsedMs)
        {
            switch (beat)
            {
                case BreathBeat.Inhale:
                {
                    float t = Mathf.Clamp01((float)cycleElapsedMs / BreathingCadence.InhaleMs);
                    return Mathf.Lerp(MinScale, MaxScale, t);
                }
                case BreathBeat.HoldTop:
                    return MaxScale;
                case BreathBeat.Exhale:
                {
                    int exhaleElapsed = cycleElapsedMs - BreathingCadence.TapTargetMs;
                    float t = Mathf.Clamp01((float)exhaleElapsed / BreathingCadence.ExhaleMs);
                    return Mathf.Lerp(MaxScale, MinScale, t);
                }
                default:
                    return MinScale;
            }
        }

        static Color ColorAt(BreathBeat beat, int cycleElapsedMs, Palette palette)
        {
            switch (beat)
            {
                case BreathBeat.Inhale:
                {
                    float t = Mathf.Clamp01((float)cycleElapsedMs / BreathingCadence.InhaleMs);
                    return Color.Lerp(palette.Bone, palette.Hearth, t);
                }
                case BreathBeat.HoldTop:
                    return palette.Hearth;
                case BreathBeat.Exhale:
                {
                    int exhaleElapsed = cycleElapsedMs - BreathingCadence.TapTargetMs;
                    float t = Mathf.Clamp01((float)exhaleElapsed / BreathingCadence.ExhaleMs);
                    return Color.Lerp(palette.Hearth, palette.Bone, t);
                }
                default:
                    return palette.Bone;
            }
        }
    }
}
