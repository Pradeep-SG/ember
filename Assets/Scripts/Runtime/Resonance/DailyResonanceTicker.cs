using System;
using System.Collections.Generic;
using System.Linq;
using Kindrith.Core;
using Kindrith.Data;
using Kindrith.Persistence;

namespace Kindrith.Resonance
{
    // Runs once per local day on app foreground. Idempotent within the same day.
    // Strong day = ≥1 oath_completed AND no chain_lapse with severity >= moderate that day.
    // Missed day = no oath_completed AND no Sanctuary used.
    // The Sanctuary counter resets at local Monday 04:00.
    public sealed class DailyResonanceTicker
    {
        readonly ResonanceMeter _meter;
        readonly IDayClock _day;
        readonly HabitLogStore _habits;
        readonly ChainStore _chains;

        DateTime _lastTickedDate; // local Date

        public DailyResonanceTicker(ResonanceMeter meter, IDayClock day,
            HabitLogStore habits, ChainStore chains)
        {
            _meter = meter ?? throw new ArgumentNullException(nameof(meter));
            _day = day ?? throw new ArgumentNullException(nameof(day));
            _habits = habits ?? throw new ArgumentNullException(nameof(habits));
            _chains = chains ?? throw new ArgumentNullException(nameof(chains));
        }

        public DateTime LastTickedDate => _lastTickedDate;

        public void TickIfNeeded()
        {
            var today = _day.LocalNow.Date;
            if (_lastTickedDate == today) return;

            // Walk back from _lastTickedDate (or yesterday if never ticked) to today,
            // applying one day's rule per local day. This handles the case where the
            // app sat in the background across multiple days.
            DateTime cursor = _lastTickedDate == default ? today.AddDays(-1) : _lastTickedDate;

            while (cursor < today)
            {
                cursor = cursor.AddDays(1);
                if (cursor.DayOfWeek == DayOfWeek.Monday) _meter.ResetSanctuaryWeeklyCounter();

                var prevDay = cursor.AddDays(-1);
                if (IsSanctuaryUsedFor(prevDay))
                {
                    // Sanctuary halts decay for that day; no-op.
                    continue;
                }

                if (IsStrongDay(prevDay)) _meter.RecordStrongDay();
                else if (IsMissedDay(prevDay)) _meter.RecordMissedDay();
            }

            _lastTickedDate = today;
        }

        bool IsStrongDay(DateTime localDate)
        {
            return HabitsOnDay(localDate).Any(h => h.kind == "oath_completed")
                && !HabitsOnDay(localDate).Any(h => h.kind == "chain_lapse" && SeverityRank(h.severity) >= 2);
        }

        bool IsMissedDay(DateTime localDate)
        {
            return !HabitsOnDay(localDate).Any(h => h.kind == "oath_completed");
        }

        bool IsSanctuaryUsedFor(DateTime localDate)
        {
            var used = _meter.State.last_sanctuary_used_at;
            if (string.IsNullOrEmpty(used)) return false;
            if (!DateTime.TryParse(used, out var t)) return false;
            return t.ToLocalTime().Date == localDate.Date;
        }

        IEnumerable<HabitLogEntry> HabitsOnDay(DateTime localDate)
        {
            // Read every entry across all month shards. Diary-study volume is small;
            // optimize later if hot.
            foreach (var id in _habits.ListIds())
            {
                var entry = _habits.Load(id);
                if (entry == null || string.IsNullOrEmpty(entry.logged_at)) continue;
                if (!DateTime.TryParse(entry.logged_at, out var t)) continue;
                if (t.ToLocalTime().Date == localDate.Date) yield return entry;
            }
        }

        static int SeverityRank(string severity)
        {
            switch (severity)
            {
                case "minor": return 1;
                case "moderate": return 2;
                case "major": return 3;
                default: return 0;
            }
        }
    }
}
