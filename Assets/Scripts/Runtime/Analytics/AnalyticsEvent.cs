using System.Collections.Generic;

namespace Kindrith.Analytics
{
    public sealed class AnalyticsEvent
    {
        public string Name;
        public string PlayerId;
        public string SessionId;
        public string AppVersion;
        public string Platform;
        public string BuildType;
        public string Locale;
        public string TimestampLocal;
        public string TimestampUtc;
        public IDictionary<string, object> Parameters;
    }
}
