using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Kindrith.Core;
using Kindrith.Data;
using Kindrith.Persistence;

namespace Kindrith.Tests.EditMode
{
    public class EntityStoreTests
    {
        // We can't redirect Application.persistentDataPath at runtime without subclassing
        // the stores, so these tests touch the real directory and clean up after themselves.

        [Test]
        public void OathStore_RoundTripsRecord()
        {
            var store = new OathStore();
            var id = Ulid.New();
            var oath = new OathRecord
            {
                id = id,
                title = "Read 20 minutes",
                why = "I want to be a person who thinks for myself.",
                created_at = DateTime.UtcNow.ToString("o"),
                class_id = "scholar",
            };
            try
            {
                store.Save(id, oath);
                var loaded = store.Load(id);
                Assert.IsNotNull(loaded);
                Assert.AreEqual("Read 20 minutes", loaded.title);
                Assert.AreEqual("scholar", loaded.class_id);
                Assert.IsFalse(string.IsNullOrEmpty(loaded.updated_at), "Save should stamp updated_at");
            }
            finally
            {
                store.Delete(id);
            }
        }

        [Test]
        public void OathStore_ListIds_ReturnsUlidSorted()
        {
            var store = new OathStore();
            var ids = new[] { Ulid.New(), Ulid.New(), Ulid.New() };
            try
            {
                foreach (var id in ids)
                {
                    store.Save(id, new OathRecord { id = id, title = "x" });
                }
                var listed = store.ListIds().Where(x => Array.IndexOf(ids, x) >= 0).ToArray();
                CollectionAssert.AreEqual(ids, listed);
            }
            finally
            {
                foreach (var id in ids) store.Delete(id);
            }
        }

        [Test]
        public void WardenStore_LoadOrCreate_ReturnsDefaultsWhenMissing()
        {
            var path = new WardenStore().Path;
            // Stash any existing warden.json so the test runs from clean.
            var backup = path + ".testbackup";
            bool hadExisting = File.Exists(path);
            try
            {
                if (hadExisting) File.Move(path, backup);
                var w = new WardenStore().LoadOrCreate();
                Assert.IsNotNull(w);
                Assert.AreEqual(1, w.level);
                Assert.AreEqual(SchemaVersion.Warden, w.schema_version);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
                if (hadExisting && File.Exists(backup)) File.Move(backup, path);
            }
        }

        [Test]
        public void HabitLogStore_ShardsByMonth()
        {
            var store = new HabitLogStore();
            var id = Ulid.New();
            var loggedAt = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);
            var entry = new HabitLogEntry
            {
                id = id,
                kind = "oath_completed",
                logged_at = loggedAt.ToString("o"),
            };
            try
            {
                store.Save(id, entry);
                var expectedShard = Path.Combine(store.Folder, "2026-07");
                Assert.IsTrue(Directory.Exists(expectedShard), "expected month-shard folder");
                Assert.IsTrue(File.Exists(Path.Combine(expectedShard, id + ".json")));
                var loaded = store.Load(id);
                Assert.IsNotNull(loaded);
                Assert.AreEqual("oath_completed", loaded.kind);
            }
            finally
            {
                store.Delete(id);
            }
        }
    }
}
