using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
using Kindrith.Breathing;
using Kindrith.Core;
using Kindrith.Dialogue;
using Kindrith.ShadowBattle;

namespace Kindrith.Tests.PlayMode
{
    // WP-04 stub. Will be expanded in WP-07 to cover the full Phase1→Phase2→Phase3→Outcome flow.
    public class ShadowBattleE2E
    {
        sealed class RecordingEmitter : IBattleEventEmitter
        {
            public readonly List<(string name, IDictionary<string, object> parameters)> Events =
                new List<(string, IDictionary<string, object>)>();

            public void Emit(string name, IDictionary<string, object> parameters)
            {
                Events.Add((name, parameters));
            }
        }

        [UnityTest]
        public IEnumerator Phase1_TimesOutAt90s_AdvancesToPhase2_WithBeadsRemaining()
        {
            var fakeClock = new FakeClock();
            var breathingClock = new BreathingClock(fakeClock);
            breathingClock.Start();

            var emitter = new RecordingEmitter();
            var sm = new BattleStateMachine(emitter);
            sm.StartBattle(ArchetypeId.PermissionGiver);

            var beads = sm.Context.DemonBeads;
            var phase1 = new Phase1Controller(
                fakeClock, breathingClock, new TapEvaluator(), beads, sm.Context, emitter, sm.Advance);
            phase1.Start();

            // Advance scripted clock through 90_000 ms in 1s chunks.
            while (fakeClock.NowMs < 90_000)
            {
                fakeClock.Advance(1_000);
                breathingClock.Tick();
                phase1.Tick();
            }

            Assert.AreEqual(BattlePhase.Phase2, sm.Current,
                "Phase1 should auto-advance to Phase2 after 90_000 ms");
            Assert.AreEqual(90_000, sm.Context.Phase1ElapsedMs);
            Assert.AreEqual(5, beads.Remaining,
                "No taps registered, so all five beads should remain");

            var phase2Entry = emitter.Events.Last(e =>
                e.name == "shadow_battle_phase_entered" &&
                e.parameters.TryGetValue("phase", out var p) && p.ToString() == "Phase2");
            var entryCtx = (IDictionary<string, object>)phase2Entry.parameters["entry_context"];
            Assert.AreEqual(5, entryCtx["beads_remaining"]);

            yield return null;
        }
    }
}
