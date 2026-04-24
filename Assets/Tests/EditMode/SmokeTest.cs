using NUnit.Framework;

namespace Kindrith.Tests.EditMode
{
    public class SmokeTest
    {
        [Test]
        public void Arithmetic_Holds()
        {
            Assert.AreEqual(2, 1 + 1);
        }

        [Test]
        public void BreathingCadence_SumsTo19Seconds()
        {
            const int inhaleMs = 4_000;
            const int holdMs = 7_000;
            const int exhaleMs = 8_000;
            Assert.AreEqual(19_000, inhaleMs + holdMs + exhaleMs);
        }
    }
}
