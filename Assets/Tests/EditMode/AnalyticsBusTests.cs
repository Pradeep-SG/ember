using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Kindrith.Analytics;

namespace Kindrith.Tests.EditMode
{
    public class AnalyticsBusTests
    {
        sealed class FakeEnvelope : IEnvelopeProvider
        {
            public string PlayerId { get; set; } = "player_01";
            public string SessionId { get; set; } = "session_01";
            public string AppVersion { get; set; } = "0.1.0";
            public string Platform { get; set; } = "ios";
            public string BuildType { get; set; } = "debug";
            public string Locale { get; set; } = "en-US";
        }

        sealed class CapturingSink : IAnalyticsSink
        {
            public readonly List<AnalyticsEvent> Events = new List<AnalyticsEvent>();
            public void Emit(AnalyticsEvent ev) => Events.Add(ev);
        }

        [Serializable]
        sealed class EnvelopeDto
        {
            public string name;
            public string player_id;
            public string session_id;
            public string app_version;
            public string platform;
            public string build_type;
            public string locale;
            public string timestamp_local;
            public string timestamp_utc;
        }

        [Test]
        public void Emit_AttachesAllEnvelopeFields()
        {
            var sink = new CapturingSink();
            var bus = new AnalyticsBus(sink, new FakeEnvelope());

            bus.Emit("app_opened", new Dictionary<string, object> { ["resume_reason"] = "cold_start" });

            Assert.AreEqual(1, sink.Events.Count);
            var ev = sink.Events[0];
            Assert.AreEqual("app_opened", ev.Name);
            Assert.AreEqual("player_01", ev.PlayerId);
            Assert.AreEqual("session_01", ev.SessionId);
            Assert.AreEqual("0.1.0", ev.AppVersion);
            Assert.AreEqual("ios", ev.Platform);
            Assert.AreEqual("debug", ev.BuildType);
            Assert.AreEqual("en-US", ev.Locale);
            Assert.IsFalse(string.IsNullOrEmpty(ev.TimestampLocal));
            Assert.IsFalse(string.IsNullOrEmpty(ev.TimestampUtc));
            Assert.AreEqual("cold_start", ev.Parameters["resume_reason"]);
        }

        [Test]
        public void Emit_ThrowsWhenAnyEnvelopeKeyIsMissing()
        {
            var sink = new CapturingSink();
            var envelope = new FakeEnvelope { PlayerId = null };
            var bus = new AnalyticsBus(sink, envelope);

            Assert.Throws<InvalidOperationException>(() =>
                bus.Emit("app_opened", new Dictionary<string, object>()));

            envelope.PlayerId = "player_01";
            envelope.SessionId = "";
            Assert.Throws<InvalidOperationException>(() =>
                bus.Emit("app_opened", new Dictionary<string, object>()));

            envelope.SessionId = "session_01";
            envelope.AppVersion = null;
            Assert.Throws<InvalidOperationException>(() =>
                bus.Emit("app_opened", new Dictionary<string, object>()));

            envelope.AppVersion = "0.1.0";
            envelope.Platform = null;
            Assert.Throws<InvalidOperationException>(() =>
                bus.Emit("app_opened", new Dictionary<string, object>()));

            envelope.Platform = "ios";
            envelope.BuildType = null;
            Assert.Throws<InvalidOperationException>(() =>
                bus.Emit("app_opened", new Dictionary<string, object>()));

            envelope.BuildType = "debug";
            envelope.Locale = null;
            Assert.Throws<InvalidOperationException>(() =>
                bus.Emit("app_opened", new Dictionary<string, object>()));
        }

        [Test]
        public void Emit_ThrowsOnEmptyEventName()
        {
            var bus = new AnalyticsBus(new CapturingSink(), new FakeEnvelope());
            Assert.Throws<ArgumentException>(() => bus.Emit("", null));
            Assert.Throws<ArgumentException>(() => bus.Emit(null, null));
        }

        [Test]
        public void NdjsonSink_RoundTripsHundredEmits()
        {
            var path = Path.Combine(Path.GetTempPath(), $"kindrith_ndjson_{Guid.NewGuid():N}.ndjson");
            try
            {
                var sink = new NdjsonAnalyticsSink(path);
                var bus = new AnalyticsBus(sink, new FakeEnvelope());

                for (int i = 0; i < 100; i++)
                {
                    bus.Emit("test_event", new Dictionary<string, object>
                    {
                        ["index"] = i,
                        ["payload"] = "value_" + i,
                    });
                }

                var contents = File.ReadAllText(path);
                Assert.IsTrue(contents.EndsWith("\n"), "file content must end with newline");

                var lines = contents.Split('\n');
                // 100 records produce 100 lines + a trailing empty after the final \n.
                Assert.AreEqual(101, lines.Length);
                for (int i = 0; i < 100; i++)
                {
                    var line = lines[i];
                    Assert.IsTrue(line.StartsWith("{"), $"line {i} should start with {{");
                    Assert.IsTrue(line.EndsWith("}"), $"line {i} should end with }}");
                    var dto = JsonUtility.FromJson<EnvelopeDto>(line);
                    Assert.IsNotNull(dto, $"line {i} did not parse");
                    Assert.AreEqual("test_event", dto.name);
                    Assert.AreEqual("player_01", dto.player_id);
                    Assert.AreEqual("session_01", dto.session_id);
                    Assert.IsFalse(string.IsNullOrEmpty(dto.timestamp_utc));
                }
                Assert.AreEqual("", lines[100], "trailing line after final \\n must be empty");
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
