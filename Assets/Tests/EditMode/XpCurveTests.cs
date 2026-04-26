using NUnit.Framework;
using UnityEngine;
using Kindrith.Progression;

namespace Kindrith.Tests.EditMode
{
    public class XpCurveTests
    {
        [Test]
        public void DefaultCurve_HasExpectedShape()
        {
            var t = ScriptableObject.CreateInstance<ProgressionTuning>();
            Assert.AreEqual(12, t.XpToReachLevel.Length, "12 entries: index 0 unused, 1..10 mapped, 11 is the cap slice");
            Assert.AreEqual(0, t.XpToReachLevel[1], "Lv 1 starts at 0 cumulative XP");
        }

        [Test]
        public void DefaultCurve_IsMonotonicallyNonDecreasing()
        {
            var t = ScriptableObject.CreateInstance<ProgressionTuning>();
            for (int i = 1; i < t.XpToReachLevel.Length - 1; i++)
            {
                Assert.That(t.XpToReachLevel[i + 1], Is.GreaterThanOrEqualTo(t.XpToReachLevel[i]),
                    $"XpToReachLevel[{i + 1}] should be >= [{i}]");
            }
        }

        [Test]
        public void DefaultCurve_Lv10Reaches2050()
        {
            var t = ScriptableObject.CreateInstance<ProgressionTuning>();
            Assert.AreEqual(2050, t.XpToReachLevel[ProgressionTuning.MaxLevel],
                "Lv 10 cap targets ~14 days of average play (5 quests/day × ~2 weeks + a few battles)");
        }

        [Test]
        public void DefaultCurve_Lv11SliceCapsAtLv10()
        {
            var t = ScriptableObject.CreateInstance<ProgressionTuning>();
            Assert.AreEqual(t.XpToReachLevel[10], t.XpToReachLevel[11],
                "Lv 11 cap matches Lv 10; XP overflow is clamped at the slice ceiling");
        }

        [Test]
        public void LevelFor_BoundaryValues()
        {
            var t = ScriptableObject.CreateInstance<ProgressionTuning>();
            Assert.AreEqual(1, t.LevelFor(0));
            Assert.AreEqual(1, t.LevelFor(99));
            Assert.AreEqual(2, t.LevelFor(100));
            Assert.AreEqual(2, t.LevelFor(219));
            Assert.AreEqual(3, t.LevelFor(220));
            Assert.AreEqual(10, t.LevelFor(2050));
            Assert.AreEqual(10, t.LevelFor(99999));
        }

        [Test]
        public void XpPerSource_Defaults()
        {
            var t = ScriptableObject.CreateInstance<ProgressionTuning>();
            Assert.AreEqual(25, t.XpPerOathCompletion);
            Assert.AreEqual(60, t.XpPerShadowBattleWin);
            Assert.AreEqual(100, t.XpPerCriticalWin);
        }
    }
}
