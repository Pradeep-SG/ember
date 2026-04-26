using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Kindrith.Persistence
{
    public sealed class BattleStore
    {
        public const string DefaultRelativeFolder = "battles";

        readonly string _folder;

        public BattleStore() : this(Path.Combine(Application.persistentDataPath, DefaultRelativeFolder)) { }

        public BattleStore(string folder)
        {
            _folder = folder;
            if (!Directory.Exists(_folder)) Directory.CreateDirectory(_folder);
        }

        public string Folder => _folder;

        public void Save(BattleRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            if (string.IsNullOrEmpty(record.id)) throw new ArgumentException("record.id required", nameof(record));

            // Phase-3 sync requirement (data-model §12) — every save bumps updated_at.
            record.updated_at = DateTime.UtcNow.ToString("o");
            if (record.schema_version <= 0) record.schema_version = 1;

            var path = Path.Combine(_folder, record.id + ".json");
            AtomicJsonStore.WriteAtomic(path, record, prettyPrint: true);
        }

        public BattleRecord[] LoadRecent(int limit)
        {
            if (limit <= 0 || !Directory.Exists(_folder)) return Array.Empty<BattleRecord>();
            var files = new DirectoryInfo(_folder).GetFiles("*.json");
            return files
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .Take(limit)
                .Select(f => JsonUtility.FromJson<BattleRecord>(File.ReadAllText(f.FullName)))
                .Where(r => r != null)
                .ToArray();
        }

        // Returns true iff any record's chain_id matches. Used by Onboarding to detect
        // tutorial-battle completion without storing a flag.
        public bool AnyForChain(string chainId)
        {
            if (string.IsNullOrEmpty(chainId) || !Directory.Exists(_folder)) return false;
            foreach (var f in Directory.GetFiles(_folder, "*.json"))
            {
                var record = JsonUtility.FromJson<BattleRecord>(File.ReadAllText(f));
                if (record != null && record.chain_id == chainId) return true;
            }
            return false;
        }
    }
}
