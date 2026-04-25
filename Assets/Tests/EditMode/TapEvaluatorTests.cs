using NUnit.Framework;
using Kindrith.ShadowBattle;

namespace Kindrith.Tests.EditMode
{
    public class TapEvaluatorTests
    {
        // 12 cases: all four grades, both early and late sides, including spec boundary points.
        [TestCase(11_000, TapGrade.Perfect, 0,    TestName = "Center is Perfect")]
        [TestCase(11_140, TapGrade.Perfect, 140,  TestName = "Late within Perfect window")]
        [TestCase(11_160, TapGrade.Clean,   160,  TestName = "Late just outside Perfect → Clean")]
        [TestCase(11_400, TapGrade.Clean,   400,  TestName = "Late at Clean window upper bound")]
        [TestCase(11_410, TapGrade.Loose,   410,  TestName = "Late just outside Clean → Loose")]
        [TestCase(11_900, TapGrade.Loose,   900,  TestName = "Late at Loose window upper bound")]
        [TestCase(11_910, TapGrade.Miss,    910,  TestName = "Late beyond Loose → Miss")]
        [TestCase(10_860, TapGrade.Perfect, -140, TestName = "Early within Perfect window")]
        [TestCase(10_840, TapGrade.Clean,   -160, TestName = "Early just outside Perfect → Clean")]
        [TestCase(10_600, TapGrade.Clean,   -400, TestName = "Early at Clean window upper bound")]
        [TestCase(10_590, TapGrade.Loose,   -410, TestName = "Early just outside Clean → Loose")]
        [TestCase(10_090, TapGrade.Miss,    -910, TestName = "Early beyond Loose → Miss")]
        public void Grade_TableMatchesSpec(int cycleElapsedMs, TapGrade expected, int expectedOffset)
        {
            var eval = new TapEvaluator();
            var grade = eval.Grade(cycleElapsedMs, out int offset);
            Assert.AreEqual(expected, grade);
            Assert.AreEqual(expectedOffset, offset);
        }

        [TestCase(TapGrade.Perfect, 2)]
        [TestCase(TapGrade.Clean,   1)]
        [TestCase(TapGrade.Loose,   0)]
        [TestCase(TapGrade.Miss,    0)]
        public void DamageFor_MatchesSpec(TapGrade grade, int expected)
        {
            Assert.AreEqual(expected, TapEvaluator.DamageFor(grade));
        }

        [Test]
        public void CustomTapTarget_ShiftsBoundaries()
        {
            var eval = new TapEvaluator(tapTargetMs: 5_000);
            var grade = eval.Grade(5_000, out int offset);
            Assert.AreEqual(TapGrade.Perfect, grade);
            Assert.AreEqual(0, offset);

            grade = eval.Grade(5_410, out offset);
            Assert.AreEqual(TapGrade.Loose, grade);
            Assert.AreEqual(410, offset);
        }
    }
}
