using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Kindrith.Breathing;

namespace Kindrith.Tests.PlayMode
{
    public class ShadowBattleSceneSmokeTest
    {
        [UnityTest]
        public IEnumerator SceneLoads_CircleExists_ClockRunning()
        {
            var op = EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Scenes/ShadowBattle.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return op;

            // Let Awake/Start finish.
            yield return null;
            yield return null;

            var harness = Object.FindFirstObjectByType<BreathingHarness>();
            Assert.IsNotNull(harness, "BreathingHarness component missing from ShadowBattle scene");
            Assert.IsNotNull(harness.Clock, "BreathingHarness.Clock not initialized");

            // Sample over a short interval to verify the clock advances.
            int initialElapsed = harness.Clock.CycleElapsedMs;
            yield return new WaitForSeconds(0.2f);
            int laterElapsed = harness.Clock.CycleElapsedMs;

            Assert.Greater(laterElapsed, initialElapsed,
                "BreathingClock did not advance — Update tick wiring broken");
        }
    }
}
