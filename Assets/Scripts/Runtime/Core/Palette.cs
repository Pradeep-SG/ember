using UnityEngine;

namespace Kindrith.Core
{
    [CreateAssetMenu(fileName = "KindrithPalette", menuName = "Kindrith/Palette", order = 0)]
    public sealed class Palette : ScriptableObject
    {
        // Core palette — source of truth in docs/art-direction.md
        public Color Hearth = new Color(0.9490f, 0.3529f, 0.1098f, 1f);    // #F25A1C
        public Color Duskwine = new Color(0.2313f, 0.1215f, 0.2470f, 1f);  // #3B1F3F
        public Color Bone = new Color(0.9294f, 0.8823f, 0.8000f, 1f);      // #EDE1CC
        public Color Graphite = new Color(0.1019f, 0.0941f, 0.1333f, 1f);  // #1A1822
        public Color Moss = new Color(0.4313f, 0.5450f, 0.3607f, 1f);      // #6E8B5C
    }
}
