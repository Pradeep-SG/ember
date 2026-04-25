using System;
using System.Globalization;
using UnityEngine;

namespace Kindrith.Analytics
{
    public sealed class DefaultEnvelopeProvider : IEnvelopeProvider
    {
        const string PlayerIdPrefKey = "kindrith.player_id";

        public DefaultEnvelopeProvider()
        {
            SessionId = Guid.NewGuid().ToString("N");

            if (PlayerPrefs.HasKey(PlayerIdPrefKey))
            {
                PlayerId = PlayerPrefs.GetString(PlayerIdPrefKey);
            }
            else
            {
                PlayerId = Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(PlayerIdPrefKey, PlayerId);
                PlayerPrefs.Save();
            }
        }

        public string PlayerId { get; }
        public string SessionId { get; }
        public string AppVersion => string.IsNullOrEmpty(Application.version) ? "0.0.0" : Application.version;
        public string Platform => "ios";
        public string BuildType => Debug.isDebugBuild ? "debug" : "release";
        public string Locale => CultureInfo.CurrentCulture.Name;
    }
}
