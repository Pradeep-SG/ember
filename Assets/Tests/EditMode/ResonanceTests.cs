using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Kindrith.Core;
using Kindrith.Data;
using Kindrith.Persistence;
using Kindrith.Resonance;

namespace Kindrith.Tests.EditMode
{
    public class ResonanceTests
    {
        // Touches the real resonance.json path; backs it up around each test.
        // Same pattern as WardenStore_LoadOrCreate_ReturnsDefaultsWhenMissing in WP-09.
        ResonanceTuning _tuning;
        string _path;
        string _backup;
        bool _hadExisting;

        [SetUp]
        public void Setup()
        {
            _tuning = ScriptableObject.CreateInstance<ResonanceTuning>();
            // Defaults match the brief.
            _path = new ResonanceStore().Path;
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

        ResonanceMeter NewMeter() => new ResonanceMeter(new ResonanceStore(), _tuning);

        [Test]
        public void NewMeter_StartsAtDimZero()
        {
            var m = NewMeter();
            Assert.AreEqual(0f, m.CurrentValue);
            Assert.AreEqual(ResonanceTier.Dim, m.CurrentTier);
        }

        [Test]
        public void StrongDayCrossesDimToWarmAt25Point01()
        {
            var m = NewMeter();
            ResonanceTier captured = ResonanceTier.Dim;
            m.TierChanged += (from, to) => captured = to;
            // 7 strong days × 4 = 28, crosses 25 between day 7's gain.
            for (int i = 0; i < 7; i++) m.RecordStrongDay();
            Assert.AreEqual(28f, m.CurrentValue);
            Assert.AreEqual(ResonanceTier.Warm, m.CurrentTier);
            Assert.AreEqual(ResonanceTier.Warm, captured);
        }

        [Test]
        public void TierTransitions_DimWarmBrightRadiant()
        {
            // TierFor breakpoints: <=25 Dim, <=60 Warm, <=85 Bright, else Radiant.
            // Step values are picked off the boundary so the assertions don't sit on
            // the exact breakpoint (where 25 stays Dim, 60 stays Warm, etc.).
            var m = NewMeter();
            for (int i = 0; i < 7; i++) m.RecordStrongDay(); // 28 → Warm
            Assert.AreEqual(ResonanceTier.Warm, m.CurrentTier);
            for (int i = 0; i < 9; i++) m.RecordStrongDay(); // 64 → Bright
            Assert.AreEqual(ResonanceTier.Bright, m.CurrentTier);
            for (int i = 0; i < 7; i++) m.RecordStrongDay(); // 92 → Radiant
            Assert.AreEqual(ResonanceTier.Radiant, m.CurrentTier);
        }

        [Test]
        public void DecayClampsAtZero()
        {
            var m = NewMeter();
            for (int i = 0; i < 50; i++) m.RecordMissedDay();
            Assert.AreEqual(0f, m.CurrentValue);
        }

        [Test]
        public void GainClampsAt100()
        {
            var m = NewMeter();
            for (int i = 0; i < 50; i++) m.RecordStrongDay();
            Assert.AreEqual(100f, m.CurrentValue);
        }

        [Test]
        public void Sanctuary_DecrementsCounterAndStampsTimestamp()
        {
            var m = NewMeter();
            Assert.AreEqual(1, m.SanctuaryDaysRemainingThisWeek);
            m.UseSanctuary();
            Assert.AreEqual(0, m.SanctuaryDaysRemainingThisWeek);
            Assert.IsFalse(string.IsNullOrEmpty(m.State.last_sanctuary_used_at));
        }

        [Test]
        public void Sanctuary_CapsAtZero()
        {
            var m = NewMeter();
            m.UseSanctuary();
            m.UseSanctuary();
            m.UseSanctuary();
            Assert.AreEqual(0, m.SanctuaryDaysRemainingThisWeek);
        }

        [Test]
        public void ResetSanctuaryWeeklyCounter_RestoresMax()
        {
            var m = NewMeter();
            m.UseSanctuary();
            Assert.AreEqual(0, m.SanctuaryDaysRemainingThisWeek);
            m.ResetSanctuaryWeeklyCounter();
            Assert.AreEqual(1, m.SanctuaryDaysRemainingThisWeek);
        }

        [Test]
        public void DailyTicker_IsIdempotentWithinSameDay()
        {
            var meter = NewMeter();
            for (int i = 0; i < 5; i++) meter.RecordStrongDay(); // start at 20
            var startValue = meter.CurrentValue;

            var day = new FakeDayClock();
            day.SetLocalNow(new DateTime(2026, 4, 26, 14, 0, 0, DateTimeKind.Local));
            var ticker = new DailyResonanceTicker(meter, day, new HabitLogStore(), new ChainStore());

            ticker.TickIfNeeded();
            for (int i = 0; i < 12; i++) ticker.TickIfNeeded();

            // No habits, no chains → "yesterday" was a missed day → applies once.
            Assert.AreEqual(startValue - _tuning.DecayPerMissedDay, meter.CurrentValue, 0.001f);
        }

        [Test]
        public void DailyTicker_AppliesPerDayAcrossGap()
        {
            var meter = NewMeter();
            for (int i = 0; i < 10; i++) meter.RecordStrongDay(); // 40
            var day = new FakeDayClock();
            day.SetLocalNow(new DateTime(2026, 4, 24, 14, 0, 0, DateTimeKind.Local));

            var ticker = new DailyResonanceTicker(meter, day, new HabitLogStore(), new ChainStore());
            ticker.TickIfNeeded(); // baseline anchor
            var anchor = meter.CurrentValue;

            // Jump 3 days forward.
            day.SetLocalNow(new DateTime(2026, 4, 27, 14, 0, 0, DateTimeKind.Local));
            ticker.TickIfNeeded();

            // Three missed days → ~3 × 6 = 18 decay.
            Assert.That(meter.CurrentValue, Is.LessThan(anchor));
            Assert.That(anchor - meter.CurrentValue, Is.GreaterThanOrEqualTo(_tuning.DecayPerMissedDay * 3 - 0.5f));
        }
    }
}
