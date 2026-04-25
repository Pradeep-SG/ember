using System;
using System.Collections.Generic;

namespace Kindrith.Analytics
{
    public sealed class AnalyticsBus
    {
        readonly IAnalyticsSink _sink;
        readonly IEnvelopeProvider _envelope;

        public AnalyticsBus(IAnalyticsSink sink, IEnvelopeProvider envelope)
        {
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _envelope = envelope ?? throw new ArgumentNullException(nameof(envelope));
        }

        public void Emit(string name, IDictionary<string, object> parameters = null)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("event name required", nameof(name));

            ValidateEnvelope();

            var ev = new AnalyticsEvent
            {
                Name = name,
                PlayerId = _envelope.PlayerId,
                SessionId = _envelope.SessionId,
                AppVersion = _envelope.AppVersion,
                Platform = _envelope.Platform,
                BuildType = _envelope.BuildType,
                Locale = _envelope.Locale,
                TimestampLocal = DateTime.Now.ToString("o"),
                TimestampUtc = DateTime.UtcNow.ToString("o"),
                Parameters = parameters ?? new Dictionary<string, object>(),
            };
            _sink.Emit(ev);
        }

        void ValidateEnvelope()
        {
            if (string.IsNullOrEmpty(_envelope.PlayerId))   throw new InvalidOperationException("envelope player_id missing");
            if (string.IsNullOrEmpty(_envelope.SessionId))  throw new InvalidOperationException("envelope session_id missing");
            if (string.IsNullOrEmpty(_envelope.AppVersion)) throw new InvalidOperationException("envelope app_version missing");
            if (string.IsNullOrEmpty(_envelope.Platform))   throw new InvalidOperationException("envelope platform missing");
            if (string.IsNullOrEmpty(_envelope.BuildType))  throw new InvalidOperationException("envelope build_type missing");
            if (string.IsNullOrEmpty(_envelope.Locale))     throw new InvalidOperationException("envelope locale missing");
        }
    }
}
