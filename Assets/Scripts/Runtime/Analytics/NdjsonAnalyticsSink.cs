using System.IO;
using UnityEngine;

namespace Kindrith.Analytics
{
    public sealed class NdjsonAnalyticsSink : IAnalyticsSink
    {
        public const string DefaultRelativePath = "analytics/p1-events.ndjson";

        readonly string _filePath;

        public NdjsonAnalyticsSink() : this(Path.Combine(Application.persistentDataPath, DefaultRelativePath)) { }

        public NdjsonAnalyticsSink(string filePath)
        {
            _filePath = filePath;
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        public string FilePath => _filePath;

        public void Emit(AnalyticsEvent ev)
        {
            var line = JsonWriter.Write(ev);
            File.AppendAllText(_filePath, line + "\n");
        }
    }
}
