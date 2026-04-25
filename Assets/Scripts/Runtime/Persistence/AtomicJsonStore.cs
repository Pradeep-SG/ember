using System.IO;
using UnityEngine;

namespace Kindrith.Persistence
{
    // Atomic JSON write helper: serialize to .tmp, fsync, rename. Crash mid-save leaves
    // either the previous good file or the new one — never a half-written turd.
    internal static class AtomicJsonStore
    {
        public static void WriteAtomic<T>(string path, T entity, bool prettyPrint = true)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var tmp = path + ".tmp";
            var json = JsonUtility.ToJson(entity, prettyPrint);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);

            using (var fs = File.Create(tmp))
            {
                fs.Write(bytes, 0, bytes.Length);
                fs.Flush(flushToDisk: true);
            }

            if (File.Exists(path)) File.Replace(tmp, path, null);
            else File.Move(tmp, path);
        }

        public static T ReadOrNull<T>(string path) where T : class
        {
            if (!File.Exists(path)) return null;
            var json = File.ReadAllText(path);
            return JsonUtility.FromJson<T>(json);
        }
    }
}
