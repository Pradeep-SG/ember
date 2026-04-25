using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Kindrith.Persistence;
using Kindrith.Persistence.Migrations;

namespace Kindrith.Tests.EditMode
{
    public class MigrationTests
    {
        sealed class CountingMigration : IMigration
        {
            public int Version { get; }
            public string Description => "test";
            public int AppliedCount;
            public CountingMigration(int v) { Version = v; }
            public void Apply() { AppliedCount++; }
        }

        [Test]
        public void Migrator_AppliesEachMigrationOnce()
        {
            var m1 = new CountingMigration(0);
            var m2 = new CountingMigration(1);
            var migrator = new Migrator(new IMigration[] { m1, m2 });

            // First clean any previous state.
            var statePath = migrator.StatePath;
            if (File.Exists(statePath)) File.Delete(statePath);

            try
            {
                int applied = migrator.Run();
                Assert.AreEqual(2, applied);
                Assert.AreEqual(1, m1.AppliedCount);
                Assert.AreEqual(1, m2.AppliedCount);

                // Re-running is a no-op.
                int reapplied = migrator.Run();
                Assert.AreEqual(0, reapplied);
                Assert.AreEqual(1, m1.AppliedCount);
                Assert.AreEqual(1, m2.AppliedCount);
            }
            finally
            {
                if (File.Exists(statePath)) File.Delete(statePath);
            }
        }

        [Test]
        public void M_0000_StampsLegacyBattleRecordWithSchemaAndUpdatedAt()
        {
            var folder = Path.Combine(Application.persistentDataPath, BattleStore.DefaultRelativeFolder);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            // Synthesize a Phase 1 record with schema_version=0 and no updated_at.
            var record = new BattleRecord
            {
                id = "test_legacy_" + Guid.NewGuid().ToString("N"),
                schema_version = 0,
                outcome = "win",
                trigger = "user_resist_tap",
                started_at = DateTime.UtcNow.ToString("o"),
                ended_at = DateTime.UtcNow.ToString("o"),
            };
            var path = Path.Combine(folder, record.id + ".json");

            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(record, true));

                new M_0000_Phase1Bootstrap().Apply();

                var loaded = JsonUtility.FromJson<BattleRecord>(File.ReadAllText(path));
                Assert.AreEqual(1, loaded.schema_version);
                Assert.IsFalse(string.IsNullOrEmpty(loaded.updated_at));
                Assert.AreEqual("win", loaded.outcome, "non-target fields must survive migration");
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void M_0000_IsIdempotent()
        {
            var folder = Path.Combine(Application.persistentDataPath, BattleStore.DefaultRelativeFolder);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            var record = new BattleRecord
            {
                id = "test_idempotent_" + Guid.NewGuid().ToString("N"),
                schema_version = 1,
                updated_at = "2026-04-26T00:00:00.0000000Z",
                outcome = "win",
                trigger = "user_resist_tap",
            };
            var path = Path.Combine(folder, record.id + ".json");

            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(record, true));

                new M_0000_Phase1Bootstrap().Apply();
                var loaded = JsonUtility.FromJson<BattleRecord>(File.ReadAllText(path));
                // updated_at should not have been re-stamped — it was already populated.
                Assert.AreEqual("2026-04-26T00:00:00.0000000Z", loaded.updated_at);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
