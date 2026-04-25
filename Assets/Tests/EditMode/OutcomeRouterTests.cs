using NUnit.Framework;
using Kindrith.Core;
using Kindrith.Dialogue;
using Kindrith.ShadowBattle;

namespace Kindrith.Tests.EditMode
{
    public class OutcomeRouterTests
    {
        static BattleContext MakeContext(int beats, int beadsRemaining, BattleOutcome preset = BattleOutcome.None)
        {
            var beads = new Beads();
            int damageNeeded = 5 - beadsRemaining;
            if (damageNeeded > 0) beads.Damage(damageNeeded);

            var ctx = new BattleContext(ArchetypeId.PermissionGiver, beads)
            {
                BeatsLandedInPhase3 = beats,
                Outcome = preset,
            };
            return ctx;
        }

        [TestCase(3, 5, BattleOutcome.CriticalWin, TestName = "3 beats with full beads → CriticalWin")]
        [TestCase(3, 0, BattleOutcome.CriticalWin, TestName = "3 beats with empty beads → CriticalWin")]
        [TestCase(2, 5, BattleOutcome.Win,         TestName = "2 beats with full beads → Win")]
        [TestCase(2, 0, BattleOutcome.Win,         TestName = "2 beats with empty beads → Win")]
        [TestCase(1, 0, BattleOutcome.Win,         TestName = "1 beat with empty beads → Win (P1 cleared)")]
        [TestCase(0, 0, BattleOutcome.Win,         TestName = "0 beats with empty beads → Win (P1 cleared)")]
        [TestCase(1, 3, BattleOutcome.Loss,        TestName = "1 beat with beads remaining → Loss")]
        [TestCase(0, 5, BattleOutcome.Loss,        TestName = "0 beats with full beads → Loss")]
        [TestCase(0, 1, BattleOutcome.Loss,        TestName = "0 beats with one bead → Loss")]
        public void Resolve_TruthTable(int beats, int beadsRemaining, BattleOutcome expected)
        {
            var ctx = MakeContext(beats, beadsRemaining);
            Assert.AreEqual(expected, OutcomeRouter.Resolve(ctx));
        }

        [Test]
        public void Resolve_PreservesAbandonOutcome()
        {
            // Even with 3 beats landed, an Abandon already on the context wins.
            var ctx = MakeContext(beats: 3, beadsRemaining: 5, preset: BattleOutcome.Abandon);
            Assert.AreEqual(BattleOutcome.Abandon, OutcomeRouter.Resolve(ctx));
        }

        [Test]
        public void LossCopy_HeaderMatchesSpec()
        {
            // Spec §5.2 — the header must be exactly this string. No shaming, no exclamation.
            Assert.AreEqual("The Demon is strong today.", Strings.RewardLossHeader);
        }

        [Test]
        public void LossCopy_BodyMatchesSpec()
        {
            // Spec §5.2 — exact body copy. Tests guard against accidental edits.
            Assert.AreEqual(
                "You showed up. That is the hardest part. Tomorrow you fight again.",
                Strings.RewardLossBody);
        }

        [Test]
        public void AbandonCopy_HeaderMatchesSpec()
        {
            Assert.AreEqual("Stepped back.", Strings.RewardAbandonHeader);
        }

        [Test]
        public void AbandonCopy_BodyMatchesSpec()
        {
            Assert.AreEqual(
                "The battle waits. Come back when you're ready.",
                Strings.RewardAbandonBody);
        }
    }
}
