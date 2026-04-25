using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Kindrith.Data;

namespace Kindrith.Persistence
{
    // Base for multi-entity stores (oaths, chains, demons, lorebook). Lays one JSON-per-id
    // under <persistentDataPath>/kindrith/<folder>/. ListIds returns ULID-sorted ids
    // (filesystem lexicographic order matches ULID time order).
    public abstract class FlatFolderStore<T> : IEntityStore<T> where T : class, new()
    {
        readonly string _folder;
        readonly Action<T> _stampUpdatedAt;

        protected FlatFolderStore(string folder, Action<T> stampUpdatedAt)
        {
            _folder = Path.Combine(Application.persistentDataPath, "kindrith", folder);
            _stampUpdatedAt = stampUpdatedAt;
            if (!Directory.Exists(_folder)) Directory.CreateDirectory(_folder);
        }

        public string Folder => _folder;

        public T Load(string id)
        {
            return AtomicJsonStore.ReadOrNull<T>(PathFor(id));
        }

        public void Save(string id, T entity)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("id required", nameof(id));
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            _stampUpdatedAt?.Invoke(entity);
            AtomicJsonStore.WriteAtomic(PathFor(id), entity);
        }

        public void Delete(string id)
        {
            var path = PathFor(id);
            if (File.Exists(path)) File.Delete(path);
        }

        public IEnumerable<string> ListIds()
        {
            if (!Directory.Exists(_folder)) yield break;
            var files = Directory.GetFiles(_folder, "*.json");
            Array.Sort(files, StringComparer.Ordinal);
            foreach (var file in files)
            {
                yield return Path.GetFileNameWithoutExtension(file);
            }
        }

        string PathFor(string id) => Path.Combine(_folder, id + ".json");
    }

    public sealed class OathStore : FlatFolderStore<OathRecord>
    {
        public OathStore() : base("oaths",
            r => r.updated_at = DateTime.UtcNow.ToString("o")) { }
    }

    public sealed class ChainStore : FlatFolderStore<ChainRecord>
    {
        public ChainStore() : base("chains",
            r => r.updated_at = DateTime.UtcNow.ToString("o")) { }
    }

    public sealed class DemonStore : FlatFolderStore<DemonRecord>
    {
        public DemonStore() : base("demons",
            r => r.updated_at = DateTime.UtcNow.ToString("o")) { }
    }

    public sealed class LorebookStore : FlatFolderStore<LorebookEntry>
    {
        public LorebookStore() : base("lorebook",
            r => r.updated_at = DateTime.UtcNow.ToString("o")) { }
    }

    // Habit log + shadow battles get sharded by YYYY-MM to keep folder bloat manageable.
    public abstract class MonthShardedStore<T> : IEntityStore<T> where T : class, new()
    {
        readonly string _baseFolder;
        readonly Action<T> _stampUpdatedAt;
        readonly Func<T, DateTime> _entryUtc;

        protected MonthShardedStore(string folder, Func<T, DateTime> entryUtc, Action<T> stampUpdatedAt)
        {
            _baseFolder = Path.Combine(Application.persistentDataPath, "kindrith", folder);
            _entryUtc = entryUtc;
            _stampUpdatedAt = stampUpdatedAt;
            if (!Directory.Exists(_baseFolder)) Directory.CreateDirectory(_baseFolder);
        }

        public string Folder => _baseFolder;

        public T Load(string id)
        {
            // Look across all month shards because we don't know which one holds the id.
            foreach (var shard in Directory.GetDirectories(_baseFolder))
            {
                var path = Path.Combine(shard, id + ".json");
                if (File.Exists(path)) return AtomicJsonStore.ReadOrNull<T>(path);
            }
            return null;
        }

        public void Save(string id, T entity)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("id required", nameof(id));
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            _stampUpdatedAt?.Invoke(entity);
            var shard = ShardFor(_entryUtc(entity));
            if (!Directory.Exists(shard)) Directory.CreateDirectory(shard);
            AtomicJsonStore.WriteAtomic(Path.Combine(shard, id + ".json"), entity);
        }

        public void Delete(string id)
        {
            foreach (var shard in Directory.GetDirectories(_baseFolder))
            {
                var path = Path.Combine(shard, id + ".json");
                if (File.Exists(path)) { File.Delete(path); return; }
            }
        }

        public IEnumerable<string> ListIds()
        {
            if (!Directory.Exists(_baseFolder)) yield break;
            var shards = Directory.GetDirectories(_baseFolder);
            Array.Sort(shards, StringComparer.Ordinal);
            foreach (var shard in shards)
            {
                var files = Directory.GetFiles(shard, "*.json");
                Array.Sort(files, StringComparer.Ordinal);
                foreach (var file in files)
                {
                    yield return Path.GetFileNameWithoutExtension(file);
                }
            }
        }

        string ShardFor(DateTime utc) => Path.Combine(_baseFolder, utc.ToString("yyyy-MM"));
    }

    public sealed class HabitLogStore : MonthShardedStore<HabitLogEntry>
    {
        public HabitLogStore() : base(
            "habit_log",
            entry => DateTime.TryParse(entry.logged_at, out var t) ? t.ToUniversalTime() : DateTime.UtcNow,
            entry => entry.updated_at = DateTime.UtcNow.ToString("o")) { }
    }
}
