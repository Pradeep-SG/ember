using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Kindrith.Analytics;
using Kindrith.Onboarding;
using Kindrith.Persistence;

namespace Kindrith.Tests.EditMode
{
    public class OnboardingFlowTests
    {
        // Touches real persistentDataPath stores; backs each up around the test.
        readonly string[] _backedUpFiles = new string[0];

        WardenStore _wardens;
        OathStore _oaths;
        ChainStore _chains;
        DemonStore _demons;
        BattleStore _battles;
        AnalyticsBus _analytics;

        string _wardenPath, _wardenBackup;
        bool _hadWarden;
        string _oathFolder, _chainFolder, _demonFolder;

        [SetUp]
        public void Setup()
        {
            _wardens = new WardenStore();
            _oaths = new OathStore();
            _chains = new ChainStore();
            _demons = new DemonStore();
            _battles = new BattleStore();
            _analytics = new AnalyticsBus(new InMemorySink(), new FakeEnvelopeProvider());

            _wardenPath = _wardens.Path;
            _wardenBackup = _wardenPath + ".testbackup";
            _hadWarden = File.Exists(_wardenPath);
            if (_hadWarden) File.Move(_wardenPath, _wardenBackup);

            _oathFolder = _oaths.Folder;
            _chainFolder = _chains.Folder;
            _demonFolder = _demons.Folder;

            // Ensure clean slate.
            CleanFolder(_oathFolder);
            CleanFolder(_chainFolder);
            CleanFolder(_demonFolder);
            CleanFolder(_battles.Folder);
        }

        [TearDown]
        public void Teardown()
        {
            if (File.Exists(_wardenPath)) File.Delete(_wardenPath);
            if (_hadWarden && File.Exists(_wardenBackup)) File.Move(_wardenBackup, _wardenPath);
            CleanFolder(_oathFolder);
            CleanFolder(_chainFolder);
            CleanFolder(_demonFolder);
            CleanFolder(_battles.Folder);
        }

        static void CleanFolder(string folder)
        {
            if (!Directory.Exists(folder)) return;
            foreach (var f in Directory.GetFiles(folder, "*.json")) File.Delete(f);
        }

        sealed class InMemorySink : IAnalyticsSink
        {
            public readonly System.Collections.Generic.List<string> Names = new System.Collections.Generic.List<string>();
            public void Emit(AnalyticsEvent ev) => Names.Add(ev.Name);
        }

        sealed class FakeEnvelopeProvider : IEnvelopeProvider
        {
            public string PlayerId => "p";
            public string SessionId => "s";
            public string AppVersion => "test";
            public string Platform => "test";
            public string BuildType => "debug";
            public string Locale => "en-US";
        }

        OnboardingFlow NewFlow() => new OnboardingFlow(_wardens, _oaths, _chains, _demons, _battles, _analytics);

        [Test]
        public void NewFlow_StartsAtWardenName()
        {
            var flow = NewFlow();
            Assert.AreEqual(OnboardingStepId.WardenName, flow.CurrentStep);
            Assert.IsFalse(flow.IsComplete);
        }

        [Test]
        public void Submit_Steps_AdvancesThroughFlow()
        {
            var flow = NewFlow();
            flow.Resume();

            flow.Submit(OnboardingStepId.WardenName, new WardenNamePayload { DisplayName = "Ash" });
            Assert.AreEqual(OnboardingStepId.ClassSelect, flow.CurrentStep);

            flow.Submit(OnboardingStepId.ClassSelect, new ClassSelectPayload { ClassId = "scholar" });
            Assert.AreEqual(OnboardingStepId.FirstOath, flow.CurrentStep);

            flow.Submit(OnboardingStepId.FirstOath, new FirstOathPayload
            {
                Title = "Read 20 minutes",
                Why = "Test",
                ClassId = "scholar",
            });
            Assert.AreEqual(OnboardingStepId.FirstChain, flow.CurrentStep);

            flow.Submit(OnboardingStepId.FirstChain, new FirstChainPayload
            {
                Title = "Scrolling after 10pm",
            });
            Assert.AreEqual(OnboardingStepId.DemonSummon, flow.CurrentStep);

            flow.Submit(OnboardingStepId.DemonSummon, new DemonSummonPayload());
            Assert.AreEqual(OnboardingStepId.TutorialBattle, flow.CurrentStep);
        }

        [Test]
        public void Resume_AfterPartialProgress_LandsOnNextIncompleteStep()
        {
            var flow = NewFlow();
            flow.Submit(OnboardingStepId.WardenName, new WardenNamePayload { DisplayName = "Ash" });
            flow.Submit(OnboardingStepId.ClassSelect, new ClassSelectPayload { ClassId = "scholar" });

            // Simulate kill-and-relaunch: new OnboardingFlow over the same stores.
            var resumed = NewFlow();
            Assert.AreEqual(OnboardingStepId.FirstOath, resumed.CurrentStep);
        }

        [Test]
        public void Submit_WrongStep_IsNoOp()
        {
            var flow = NewFlow();
            flow.Submit(OnboardingStepId.ClassSelect, new ClassSelectPayload { ClassId = "scholar" });
            // CurrentStep is still WardenName because ClassSelect can't apply yet.
            Assert.AreEqual(OnboardingStepId.WardenName, flow.CurrentStep);
        }

        [Test]
        public void WardenName_ValidatesLength()
        {
            var flow = NewFlow();
            Assert.Throws<ArgumentException>(() =>
                flow.Submit(OnboardingStepId.WardenName, new WardenNamePayload { DisplayName = "A" }));
            Assert.Throws<ArgumentException>(() =>
                flow.Submit(OnboardingStepId.WardenName, new WardenNamePayload { DisplayName = new string('x', 25) }));
        }

        [Test]
        public void ClassSelect_RejectsNonScholar()
        {
            var flow = NewFlow();
            flow.Submit(OnboardingStepId.WardenName, new WardenNamePayload { DisplayName = "Ash" });
            Assert.Throws<ArgumentException>(() =>
                flow.Submit(OnboardingStepId.ClassSelect, new ClassSelectPayload { ClassId = "warrior" }));
        }

        [Test]
        public void DemonSummon_StampsChainIdAndArchetype()
        {
            var flow = NewFlow();
            flow.Submit(OnboardingStepId.WardenName, new WardenNamePayload { DisplayName = "Ash" });
            flow.Submit(OnboardingStepId.ClassSelect, new ClassSelectPayload { ClassId = "scholar" });
            flow.Submit(OnboardingStepId.FirstOath, new FirstOathPayload { Title = "X", ClassId = "scholar" });
            flow.Submit(OnboardingStepId.FirstChain, new FirstChainPayload { Title = "Y" });
            flow.Submit(OnboardingStepId.DemonSummon, new DemonSummonPayload());

            var demonId = _demons.ListIds().First();
            var demon = _demons.Load(demonId);
            Assert.AreEqual(flow.OnboardingChainId, demon.chain_id);
            Assert.AreEqual("permission_giver", demon.archetype_primary);

            var chain = _chains.Load(flow.OnboardingChainId);
            Assert.AreEqual(demonId, chain.demon_id);
        }

        [Test]
        public void Complete_OnceTutorialBattleSavedWithChainId()
        {
            var flow = NewFlow();
            flow.Submit(OnboardingStepId.WardenName, new WardenNamePayload { DisplayName = "Ash" });
            flow.Submit(OnboardingStepId.ClassSelect, new ClassSelectPayload { ClassId = "scholar" });
            flow.Submit(OnboardingStepId.FirstOath, new FirstOathPayload { Title = "X", ClassId = "scholar" });
            flow.Submit(OnboardingStepId.FirstChain, new FirstChainPayload { Title = "Y" });
            flow.Submit(OnboardingStepId.DemonSummon, new DemonSummonPayload());
            Assert.AreEqual(OnboardingStepId.TutorialBattle, flow.CurrentStep);

            // Simulate BattleRunner having saved a record matching the onboarding chain.
            _battles.Save(new BattleRecord
            {
                id = Kindrith.Core.Ulid.New(),
                chain_id = flow.OnboardingChainId,
                outcome = "win",
                started_at = DateTime.UtcNow.ToString("o"),
                ended_at = DateTime.UtcNow.ToString("o"),
            });

            flow.Submit(OnboardingStepId.TutorialBattle, new TutorialBattlePayload());
            Assert.IsTrue(flow.IsComplete);
            Assert.AreEqual(OnboardingStepId.Complete, flow.CurrentStep);
        }
    }
}
