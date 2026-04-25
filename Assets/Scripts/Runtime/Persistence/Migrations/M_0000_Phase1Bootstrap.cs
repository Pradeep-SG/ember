using System;
using System.IO;
using UnityEngine;

namespace Kindrith.Persistence.Migrations
{
    // Stamp Phase 1 BattleRecords with schema_version + updated_at if absent. Idempotent —
    // re-running on already-stamped records is a no-op.
    public sealed class M_0000_Phase1Bootstrap : IMigration
    {
        public int Version => 0;
        public string Description => "Stamp Phase 1 BattleRecords with schema_version + updated_at";

        public void Apply()
        {
            var folder = Path.Combine(Application.persistentDataPath, BattleStore.DefaultRelativeFolder);
            if (!Directory.Exists(folder)) return;

            foreach (var file in Directory.GetFiles(folder, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var record = JsonUtility.FromJson<BattleRecord>(json);
                    if (record == null) continue;

                    bool dirty = false;
                    if (record.schema_version <= 0) { record.schema_version = 1; dirty = true; }
                    if (string.IsNullOrEmpty(record.updated_at))
                    {
                        record.updated_at = DateTime.UtcNow.ToString("o");
                        dirty = true;
                    }
                    if (dirty)
                    {
                        AtomicJsonStore.WriteAtomic(file, record, prettyPrint: true);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"M_0000: failed to stamp {file}: {ex.Message}");
                }
            }
        }
    }
}
