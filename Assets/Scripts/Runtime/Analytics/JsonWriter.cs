using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Kindrith.Analytics
{
    // Minimal JSON writer for AnalyticsEvent payloads. Handles primitives, strings, nested
    // dictionaries, and IEnumerable. No third-party JSON dependency per brief §1 rule 8.
    internal static class JsonWriter
    {
        public static string Write(AnalyticsEvent ev)
        {
            var sb = new StringBuilder();
            sb.Append('{');
            AppendKv(sb, "name", ev.Name); sb.Append(',');
            AppendKv(sb, "player_id", ev.PlayerId); sb.Append(',');
            AppendKv(sb, "session_id", ev.SessionId); sb.Append(',');
            AppendKv(sb, "app_version", ev.AppVersion); sb.Append(',');
            AppendKv(sb, "platform", ev.Platform); sb.Append(',');
            AppendKv(sb, "build_type", ev.BuildType); sb.Append(',');
            AppendKv(sb, "locale", ev.Locale); sb.Append(',');
            AppendKv(sb, "timestamp_local", ev.TimestampLocal); sb.Append(',');
            AppendKv(sb, "timestamp_utc", ev.TimestampUtc); sb.Append(',');
            WriteString(sb, "params"); sb.Append(':');
            WriteValue(sb, ev.Parameters);
            sb.Append('}');
            return sb.ToString();
        }

        public static string Write(IDictionary<string, object> dict)
        {
            var sb = new StringBuilder();
            WriteValue(sb, dict);
            return sb.ToString();
        }

        static void AppendKv(StringBuilder sb, string key, string value)
        {
            WriteString(sb, key);
            sb.Append(':');
            if (value == null) sb.Append("null");
            else WriteString(sb, value);
        }

        static void WriteValue(StringBuilder sb, object v)
        {
            switch (v)
            {
                case null: sb.Append("null"); return;
                case bool b: sb.Append(b ? "true" : "false"); return;
                case int i: sb.Append(i.ToString(CultureInfo.InvariantCulture)); return;
                case long l: sb.Append(l.ToString(CultureInfo.InvariantCulture)); return;
                case float f: sb.Append(f.ToString("R", CultureInfo.InvariantCulture)); return;
                case double d: sb.Append(d.ToString("R", CultureInfo.InvariantCulture)); return;
                case string s: WriteString(sb, s); return;
                case IDictionary<string, object> dict:
                    {
                        sb.Append('{');
                        bool first = true;
                        foreach (var kvp in dict)
                        {
                            if (!first) sb.Append(',');
                            first = false;
                            WriteString(sb, kvp.Key);
                            sb.Append(':');
                            WriteValue(sb, kvp.Value);
                        }
                        sb.Append('}');
                        return;
                    }
                case IEnumerable list:
                    {
                        sb.Append('[');
                        bool first = true;
                        foreach (var item in list)
                        {
                            if (!first) sb.Append(',');
                            first = false;
                            WriteValue(sb, item);
                        }
                        sb.Append(']');
                        return;
                    }
                default:
                    WriteString(sb, v.ToString());
                    return;
            }
        }

        static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"':  sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b");  break;
                    case '\f': sb.Append("\\f");  break;
                    case '\n': sb.Append("\\n");  break;
                    case '\r': sb.Append("\\r");  break;
                    case '\t': sb.Append("\\t");  break;
                    default:
                        if (c < 0x20)
                            sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else
                            sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }
}
