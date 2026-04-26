using UnityEngine;

namespace Kindrith.Progression
{
    [CreateAssetMenu(menuName = "Kindrith/Progression Tuning", fileName = "ProgressionTuning")]
    public sealed class ProgressionTuning : ScriptableObject
    {
        // Index = level reached. XpToReachLevel[1]=0, XpToReachLevel[10]=cap.
        // Index 0 unused (no Lv 0); index 11 caps the slice (Lv 11+ deferred).
        public int[] XpToReachLevel = new[]
        {
            0,    // [0] unused
            0,    // [1] starting level
            100,  // [2]
            220,  // [3]
            370,  // [4]
            550,  // [5]
            770,  // [6]
            1030, // [7]
            1330, // [8]
            1670, // [9]
            2050, // [10]
            2050, // [11] cap (Lv 11+ deferred per WP-12 out-of-scope)
        };

        public int XpPerOathCompletion = 25;
        public int XpPerShadowBattleWin = 60;
        public int XpPerCriticalWin = 100;

        public const int MaxLevel = 10;

        // Returns the level whose threshold is the highest <= cumulativeXp.
        public int LevelFor(int cumulativeXp)
        {
            for (int level = MaxLevel; level >= 1; level--)
            {
                if (cumulativeXp >= XpToReachLevel[level]) return level;
            }
            return 1;
        }
    }
}
