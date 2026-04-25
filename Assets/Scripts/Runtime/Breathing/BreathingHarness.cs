using UnityEngine;
using UnityEngine.UI;
using Kindrith.Core;

namespace Kindrith.Breathing
{
    // Dev-only host for the Harness_Breathing scene.
    // Self-builds a Canvas + Image at runtime so the .unity file stays minimal.
    // Production wiring lives in Phase1Controller (WP-04).
    public sealed class BreathingHarness : MonoBehaviour
    {
        [SerializeField] Palette _palette;
        [SerializeField] float _circleSize = 400f;

        BreathingClock _breathing;

        public BreathingClock Clock => _breathing;

        void Awake()
        {
            if (_palette == null) _palette = ScriptableObject.CreateInstance<Palette>();

            _breathing = new BreathingClock(new SystemClock());
            BuildVisuals();
        }

        void Start()
        {
            _breathing.Start();
        }

        void Update()
        {
            _breathing.Tick();
        }

        void BuildVisuals()
        {
            var canvasGo = new GameObject("HarnessCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

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
            bgGo.GetComponent<Image>().color = _palette.Graphite;

            var circleGo = new GameObject("BreathingCircle",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BreathingCircleView));
            circleGo.transform.SetParent(canvasGo.transform, false);
            var rt = (RectTransform)circleGo.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(_circleSize, _circleSize);
            rt.anchoredPosition = Vector2.zero;

            var image = circleGo.GetComponent<Image>();
            image.sprite = CreateCircleSprite(256);
            image.color = _palette.Bone;

            var view = circleGo.GetComponent<BreathingCircleView>();
            view.SetReferences(rt, image, _palette);
            view.Bind(_breathing);
        }

        // Soft-edged white circle so scaling reads as smooth and the shape looks circular.
        // The Image tints it via .color, so keep the source white.
        static Sprite CreateCircleSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var pixels = new Color32[size * size];
            float radius = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - radius + 0.5f;
                    float dy = y - radius + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - dist);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
