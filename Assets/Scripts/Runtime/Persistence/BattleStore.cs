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

            var path = Path.Combine(_folder, record.id + ".json");
            var json = JsonUtility.ToJson(record, prettyPrint: true);
            File.WriteAllText(path, json);
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
    }
}
