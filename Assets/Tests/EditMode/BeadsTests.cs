using NUnit.Framework;
using Kindrith.ShadowBattle;

namespace Kindrith.Tests.EditMode
{
    public class BeadsTests
    {
        [Test]
        public void Default_HasFiveBeadsRemaining()
        {
            var b = new Beads();
            Assert.AreEqual(5, b.Max);
            Assert.AreEqual(5, b.Remaining);
            Assert.IsFalse(b.Extinguished);
        }

        [Test]
        public void CustomMax_Honored()
        {
            var b = new Beads(max: 3);
            Assert.AreEqual(3, b.Max);
            Assert.AreEqual(3, b.Remaining);
        }

        [Test]
        public void Damage_ReducesRemainingAndFiresChangedOnce()
        {
            var b = new Beads();
            int firedCount = 0;
            int? lastValue = null;
            b.Changed += v => { firedCount++; lastValue = v; };

            b.Damage(2);

            Assert.AreEqual(3, b.Remaining);
            Assert.AreEqual(1, firedCount);
            Assert.AreEqual(3, lastValue);
        }

        [Test]
        public void Damage_ClampsToZero()
        {
            var b = new Beads(3);
            b.Damage(10);
            Assert.AreEqual(0, b.Remaining);
            Assert.IsTrue(b.Extinguished);
        }

        [Test]
        public void Extinguished_FlipsAtExactlyZero()
        {
            var b = new Beads(2);
            Assert.IsFalse(b.Extinguished);
            b.Damage(1);
            Assert.IsFalse(b.Extinguished);
            b.Damage(1);
            Assert.IsTrue(b.Extinguished);
        }

        [Test]
        public void Damage_FiresChangedOnlyOnActualStateChange()
        {
            var b = new Beads(5);
            int firedCount = 0;
            b.Changed += _ => firedCount++;

            b.Damage(1);   // 5 → 4, fires
            b.Damage(1);   // 4 → 3, fires
            b.Damage(0);   // no-op (non-positive amount)
            b.Damage(10);  // 3 → 0, fires (clamped)
            b.Damage(5);   // already 0, no fire

            Assert.AreEqual(3, firedCount);
        }

        [Test]
        public void Damage_NegativeIsNoOp()
        {
            var b = new Beads();
            int firedCount = 0;
            b.Changed += _ => firedCount++;

            b.Damage(-3);

            Assert.AreEqual(5, b.Remaining);
            Assert.AreEqual(0, firedCount);
        }
    }
}
