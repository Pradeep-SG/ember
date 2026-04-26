using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Kindrith.Persistence;
using Kindrith.Progression;
using Kindrith.ShadowBattle;

namespace Kindrith.Tests.EditMode
{
    public class LevelsTests
    {
        sealed class Recording : IBattleEventEmitter
        {
            public readonly List<(string name, IDictionary<string, object> p)> Events = new List<(string, IDictionary<string, object>)>();
            public void Emit(string name, IDictionary<string, object> parameters)
                => Events.Add((name, parameters));
        }

        ProgressionTuning _tuning;
        WardenStore _store;
        string _path;
        string _backup;
        bool _hadExisting;

        [SetUp]
        public void Setup()
        {
            _tuning = ScriptableObject.CreateInstance<ProgressionTuning>();
            _store = new WardenStore();
            _path = _store.Path;
            _backup = _path + ".testbackup";
            _hadExisting = File.Exists(_path);
            if (_hadExisting) File.Move(_path, _backup);
        }

        [TearDown]
        public void Teardown()
        {
            if (File.Exists(_path)) File.Delete(_path);
            if (_hadExisting && File.Exists(_backup)) File.Move(_backup, _path);
        }

        [Test]
        public void NewLevels_StartsAtLv1Zero()
        {
            var levels = new Levels(_store, _tuning, new Recording());
            Assert.AreEqual(1, levels.Level);
            Assert.AreEqual(0, levels.XpInLevel);
            Assert.AreEqual(100, levels.XpToNext);
        }

        [Test]
        public void GrantXp_BelowThreshold_NoLevelUp()
        {
            var emitter = new Recording();
            var levels = new Levels(_store, _tuning, emitter);
            int leveled = 0;
            levels.LeveledUp += (from, to) => leveled++;
            levels.GrantXp(50, "test");
            Assert.AreEqual(1, levels.Level);
            Assert.AreEqual(50, levels.XpInLevel);
            Assert.AreEqual(0, leveled);
            Assert.That(emitter.Events.Any(e => e.name == "xp_granted"));
        }

        [Test]
        public void GrantXp_ExactlyOnThreshold_TriggersLevelUp()
        {
            var emitter = new Recording();
            var levels = new Levels(_store, _tuning, emitter);
            int leveled = 0;
            levels.LeveledUp += (from, to) => leveled++;
            levels.GrantXp(100, "test"); // Lv 2 starts at 100
            Assert.AreEqual(2, levels.Level);
            Assert.AreEqual(0, levels.XpInLevel);
            Assert.AreEqual(1, leveled);
        }

        [Test]
        public void GrantXp_OverstepsTwoLevels_FiresTwoLeveledUpEvents()
        {
            var emitter = new Recording();
            var levels = new Levels(_store, _tuning, emitter);
            var transitions = new List<(int, int)>();
            levels.LeveledUp += (from, to) => transitions.Add((from, to));
            // 230 cumulative XP → Lv 3 (Lv 3 at 220).
            levels.GrantXp(230, "huge_grant");
            Assert.AreEqual(3, levels.Level);
            Assert.AreEqual(2, transitions.Count);
            Assert.AreEqual((1, 2), transitions[0]);
            Assert.AreEqual((2, 3), transitions[1]);
        }

        [Test]
        public void GrantXp_WrittenToWardenStore_RoundTrips()
        {
            var levels = new Levels(_store, _tuning, new Recording());
            levels.GrantXp(150, "test");
            var loaded = _store.LoadOrCreate();
            Assert.AreEqual(2, loaded.level);
            Assert.AreEqual(150, loaded.xp_total);
            Assert.AreEqual(50, loaded.xp_in_level);
        }

        [Test]
        public void GrantXp_AtLv10_ClampsAtCap()
        {
            var levels = new Levels(_store, _tuning, new Recording());
            levels.GrantXp(99_999, "huge");
            Assert.AreEqual(ProgressionTuning.MaxLevel, levels.Level);
            Assert.AreEqual(_tuning.XpToReachLevel[ProgressionTuning.MaxLevel], levels.XpTotal);
            Assert.AreEqual(0, levels.XpToNext);
        }

        [Test]
        public void Evolution_FiresAtLv10()
        {
            var emitter = new Recording();
            var levels = new Levels(_store, _tuning, emitter);
            int evolutions = 0;
            int evolvedAt = 0;
            levels.ScholarEvolved += lvl => { evolutions++; evolvedAt = lvl; };

            levels.GrantXp(_tuning.XpToReachLevel[ProgressionTuning.MaxLevel], "huge");
            Assert.AreEqual(1, evolutions);
            Assert.AreEqual(10, evolvedAt);
            Assert.AreEqual(ScholarEvolution.EvolvedOutfitId, levels.Warden.cosmetics_equipped.outfit_id);
            Assert.That(emitter.Events.Any(e => e.name == "class_evolution_triggered"));
        }

        [Test]
        public void Evolution_NotFiredAtLowerLevels()
        {
            var emitter = new Recording();
            var levels = new Levels(_store, _tuning, emitter);
            int evolutions = 0;
            levels.ScholarEvolved += _ => evolutions++;
            levels.GrantXp(1000, "test"); // ~Lv 6
            Assert.AreEqual(0, evolutions);
        }
    }
}
