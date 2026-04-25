using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Kindrith.Persistence.Migrations
{
    public interface IMigration
    {
        int Version { get; }
        string Description { get; }
        void Apply();
    }

    [Serializable]
    public sealed class MigrationState
    {
        public int last_applied_version = -1;
        public string updated_at;
    }

    public sealed class Migrator
    {
        readonly string _statePath;
        readonly IList<IMigration> _migrations;

        public Migrator(IList<IMigration> migrations)
        {
            _migrations = migrations ?? throw new ArgumentNullException(nameof(migrations));
            _statePath = Path.Combine(Application.persistentDataPath, "kindrith", "migration_state.json");
        }

        public string StatePath => _statePath;

        public int Run()
        {
            var state = AtomicJsonStore.ReadOrNull<MigrationState>(_statePath) ?? new MigrationState();
            int applied = 0;
            foreach (var m in _migrations)
            {
                if (m.Version <= state.last_applied_version) continue;
                m.Apply();
                state.last_applied_version = m.Version;
                state.updated_at = DateTime.UtcNow.ToString("o");
                AtomicJsonStore.WriteAtomic(_statePath, state);
                applied++;
            }
            return applied;
        }

        public static IList<IMigration> DefaultMigrations() => new IMigration[]
        {
            new M_0000_Phase1Bootstrap(),
        };
    }
}
