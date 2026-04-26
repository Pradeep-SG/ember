using System;
using Kindrith.Data;
using Kindrith.Persistence;

namespace Kindrith.Resonance
{
    // Reads/writes the player's resonance state (one record, ResonanceStore singleton).
    // Tier transitions emit through the TierChanged event so the meter view can repaint.
    // Decay/gain rules + Sanctuary semantics are described in docs/resonance-spec.md.
    public sealed class ResonanceMeter
    {
        readonly ResonanceStore _store;
        readonly ResonanceTuning _tuning;
        ResonanceState _state;

        public ResonanceMeter(ResonanceStore store, ResonanceTuning tuning)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
            _state = _store.LoadOrCreate();
            // First-time bootstrap: stamp tuning defaults onto the persisted state if zero.
            if (_state.sanctuary_days_max_per_week == 0) _state.sanctuary_days_max_per_week = _tuning.SanctuaryDaysMaxPerWeek;
            if (_state.sanctuary_days_remaining_this_week == 0 && string.IsNullOrEmpty(_state.last_sanctuary_used_at))
            {
                _state.sanctuary_days_remaining_this_week = _tuning.SanctuaryDaysMaxPerWeek;
            }
        }

        public float CurrentValue => _state.current_value;
        public ResonanceTier CurrentTier => _tuning.TierFor(_state.current_value);
        public int SanctuaryDaysRemainingThisWeek => _state.sanctuary_days_remaining_this_week;
        public ResonanceState State => _state;

        public event Action<ResonanceTier, ResonanceTier> TierChanged;

        public void RecordStrongDay() => Apply(_tuning.GainPerStrongDay);
        public void RecordMissedDay() => Apply(-_tuning.DecayPerMissedDay);

        public void UseSanctuary()
        {
            if (_state.sanctuary_days_remaining_this_week <= 0) return;
            _state.sanctuary_days_remaining_this_week--;
            _state.last_sanctuary_used_at = DateTime.UtcNow.ToString("o");
            PersistTier();
        }

        // Reset sanctuary count at local Monday 04:00. Caller is the daily ticker, which
        // knows the local week boundary.
        public void ResetSanctuaryWeeklyCounter()
        {
            _state.sanctuary_days_remaining_this_week = _tuning.SanctuaryDaysMaxPerWeek;
            PersistTier();
        }

        void Apply(float delta)
        {
            var before = _tuning.TierFor(_state.current_value);
            _state.current_value = Math.Max(0f, Math.Min(100f, _state.current_value + delta));
            _state.last_updated_at = DateTime.UtcNow.ToString("o");
            var after = _tuning.TierFor(_state.current_value);
            PersistTier();
            if (before != after) TierChanged?.Invoke(before, after);
        }

        void PersistTier()
        {
            _state.tier = CurrentTier.ToString().ToLowerInvariant();
            _store.Save(_state);
        }
    }
}
