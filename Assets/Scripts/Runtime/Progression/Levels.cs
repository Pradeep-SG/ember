using System;
using System.Collections.Generic;
using Kindrith.Data;
using Kindrith.Persistence;
using Kindrith.ShadowBattle;

namespace Kindrith.Progression
{
    // The Warden's level + XP. Sources: GrantXp(amount, source). Multi-level grants
    // emit one LeveledUp per level crossed (no overshoot, no batching).
    public sealed class Levels
    {
        readonly WardenStore _store;
        readonly ProgressionTuning _tuning;
        readonly IBattleEventEmitter _emitter;
        WardenRecord _warden;

        public Levels(WardenStore store, ProgressionTuning tuning, IBattleEventEmitter emitter)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
            _emitter = emitter ?? throw new ArgumentNullException(nameof(emitter));
            _warden = _store.LoadOrCreate();
            // Warden may have been minted in WP-09 with default level=1, xp_total=0.
            if (_warden.level < 1) _warden.level = 1;
        }

        public int Level => _warden.level;
        public int XpTotal => _warden.xp_total;
        public int XpInLevel => _warden.xp_in_level;
        public int XpToNext
        {
            get
            {
                if (_warden.level >= ProgressionTuning.MaxLevel) return 0;
                int floor = _tuning.XpToReachLevel[_warden.level];
                int ceil = _tuning.XpToReachLevel[_warden.level + 1];
                return Math.Max(0, ceil - floor - _warden.xp_in_level);
            }
        }

        public WardenRecord Warden => _warden;

        public event Action<int, int> LeveledUp; // (from, to)
        public event Action<int> ScholarEvolved; // newLevel (==10)

        public void GrantXp(int amount, string source)
        {
            if (amount <= 0) return;

            int beforeLevel = _warden.level;
            _warden.xp_total += amount;

            // Walk up one level at a time so each crossing emits its own LeveledUp.
            while (_warden.level < ProgressionTuning.MaxLevel
                && _warden.xp_total >= _tuning.XpToReachLevel[_warden.level + 1])
            {
                int from = _warden.level;
                _warden.level = from + 1;
                LeveledUp?.Invoke(from, _warden.level);
                if (ScholarEvolution.ShouldEvolve(_warden.level))
                {
                    ScholarEvolution.Apply(_warden);
                    ScholarEvolved?.Invoke(_warden.level);
                    _emitter.Emit("class_evolution_triggered", new Dictionary<string, object>
                    {
                        ["new_level"] = _warden.level,
                        ["outfit_id"] = _warden.cosmetics_equipped != null ? _warden.cosmetics_equipped.outfit_id : null,
                    });
                }
            }

            // Cap XP at the slice ceiling so xp_in_level stays meaningful at Lv 10.
            int cap = _tuning.XpToReachLevel[ProgressionTuning.MaxLevel];
            if (_warden.level >= ProgressionTuning.MaxLevel && _warden.xp_total > cap)
            {
                _warden.xp_total = cap;
            }

            int floor = _tuning.XpToReachLevel[_warden.level];
            _warden.xp_in_level = _warden.xp_total - floor;

            _store.Save(_warden);

            _emitter.Emit("xp_granted", new Dictionary<string, object>
            {
                ["xp_amount"] = amount,
                ["source"] = source,
                ["level_after"] = _warden.level,
                ["xp_total_after"] = _warden.xp_total,
                ["levels_crossed"] = _warden.level - beforeLevel,
            });
        }
    }
}
