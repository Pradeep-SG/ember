using NUnit.Framework;
using Kindrith.Breathing;

namespace Kindrith.Tests.EditMode
{
    public class BreathingCadenceTests
    {
        [Test]
        public void InhaleMs_MatchesSpec()
        {
            Assert.AreEqual(4_000, BreathingCadence.InhaleMs);
        }

        [Test]
        public void HoldMs_MatchesSpec()
        {
            Assert.AreEqual(7_000, BreathingCadence.HoldMs);
        }

        [Test]
        public void ExhaleMs_MatchesSpec()
        {
            Assert.AreEqual(8_000, BreathingCadence.ExhaleMs);
        }

        [Test]
        public void CycleMs_SumsTo19000()
        {
            Assert.AreEqual(19_000, BreathingCadence.CycleMs);
            Assert.AreEqual(BreathingCadence.InhaleMs + BreathingCadence.HoldMs + BreathingCadence.ExhaleMs, BreathingCadence.CycleMs);
        }

        [Test]
        public void TapTargetMs_IsHoldExhaleBoundary()
        {
            Assert.AreEqual(11_000, BreathingCadence.TapTargetMs);
            Assert.AreEqual(BreathingCadence.InhaleMs + BreathingCadence.HoldMs, BreathingCadence.TapTargetMs);
        }
    }
}
