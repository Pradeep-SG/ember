using System;
using System.Collections.Generic;
using NUnit.Framework;
using Kindrith.Core;

namespace Kindrith.Tests.EditMode
{
    public class UlidTests
    {
        [Test]
        public void New_Length_Is26()
        {
            var u = Ulid.New();
            Assert.AreEqual(26, u.Length);
        }

        [Test]
        public void New_TenThousandIds_AreAllDistinctAndMonotonic()
        {
            const int N = 10_000;
            var seen = new HashSet<string>();
            string previous = null;
            for (int i = 0; i < N; i++)
            {
                var u = Ulid.New();
                Assert.IsTrue(seen.Add(u), $"duplicate ULID at index {i}: {u}");
                if (previous != null)
                {
                    Assert.That(string.CompareOrdinal(previous, u), Is.LessThanOrEqualTo(0),
                        $"non-monotonic at index {i}: {previous} -> {u}");
                }
                previous = u;
            }
        }

        [Test]
        public void TryParse_RoundTripsTimestamp()
        {
            var u = Ulid.New();
            var beforeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Assert.IsTrue(Ulid.TryParse(u, out var ts));
            // Allow ±2s skew between New() and parse (test scheduling, not algorithmic).
            var deltaMs = Math.Abs(beforeMs - ts.ToUnixTimeMilliseconds());
            Assert.That(deltaMs, Is.LessThan(2_000));
        }

        [Test]
        public void TryParse_RejectsBadInput()
        {
            Assert.IsFalse(Ulid.TryParse(null, out _));
            Assert.IsFalse(Ulid.TryParse("", out _));
            Assert.IsFalse(Ulid.TryParse("too short", out _));
            Assert.IsFalse(Ulid.TryParse("01HW5GLILOLOLOLOLOLOLOLOLO", out _)); // I, L are illegal in Crockford
        }

        [Test]
        public void Sort_OrdinalLexicographic_Matches_TimeOrder()
        {
            var ids = new List<string>();
            for (int i = 0; i < 50; i++)
            {
                ids.Add(Ulid.New());
                System.Threading.Thread.Sleep(2);
            }
            var sorted = new List<string>(ids);
            sorted.Sort(StringComparer.Ordinal);
            CollectionAssert.AreEqual(ids, sorted);
        }
    }
}
