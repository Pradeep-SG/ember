using UnityEngine;

namespace Kindrith.Core
{
    // Thin wrapper so call sites stay free of raw UnityEngine.Debug references
    // and we can later route logs to a sink, file, or analytics without churn.
    public static class Log
    {
        public static void Info(string message) => Debug.Log(message);
        public static void Info(string message, Object context) => Debug.Log(message, context);
        public static void Warn(string message) => Debug.LogWarning(message);
        public static void Warn(string message, Object context) => Debug.LogWarning(message, context);
        public static void Error(string message) => Debug.LogError(message);
        public static void Error(string message, Object context) => Debug.LogError(message, context);
    }
}
