using System;
using System.IO;
using UnityEngine;
using Kindrith.Core;
using Kindrith.Data;

namespace Kindrith.Persistence
{
    // Base helper for the warden / resonance / realm singletons. Each lives at a fixed
    // path under <persistentDataPath>/kindrith/ per data-model §11.
    public abstract class SingletonStore<T> where T : class, new()
    {
        readonly string _path;
        readonly Action<T> _stampUpdatedAt;

        protected SingletonStore(string fileName, Action<T> stampUpdatedAt)
        {
            _path = Path.Combine(Application.persistentDataPath, "kindrith", fileName);
            _stampUpdatedAt = stampUpdatedAt;
        }

        public string Path => _path;

        public T LoadOrCreate()
        {
            var existing = AtomicJsonStore.ReadOrNull<T>(_path);
            return existing ?? new T();
        }

        public void Save(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            _stampUpdatedAt?.Invoke(entity);
            AtomicJsonStore.WriteAtomic(_path, entity);
        }
    }

    public sealed class WardenStore : SingletonStore<WardenRecord>
    {
        public WardenStore() : base("warden.json",
            r => r.updated_at = DateTime.UtcNow.ToString("o")) { }
    }

    public sealed class ResonanceStore : SingletonStore<ResonanceState>
    {
        public ResonanceStore() : base("resonance.json",
            r => r.updated_at = DateTime.UtcNow.ToString("o")) { }
    }

    public sealed class RealmStore : SingletonStore<RealmState>
    {
        public RealmStore() : base("realm.json",
            r => r.updated_at = DateTime.UtcNow.ToString("o")) { }
    }
}
