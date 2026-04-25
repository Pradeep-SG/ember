using System;

namespace Kindrith.ShadowBattle
{
    public static class OutcomeRouter
    {
        // Truth table from spec §5 / brief WP-07:
        //   Phase 3 beats = 3                                       → CriticalWin
        //   Beats = 2 OR (Beats < 2 AND beads were 0 entering P3)   → Win
        //   Beads > 0 AND beats < 2                                 → Loss
        //   ctx.Outcome already Abandon (set by state machine)      → Abandon (preserved)
        //
        // Phase 3 doesn't damage beads, so DemonBeads.Remaining at outcome time equals
        // BeadsRemaining at the moment of Phase 3 entry.
        public static BattleOutcome Resolve(BattleContext ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            if (ctx.Outcome == BattleOutcome.Abandon) return BattleOutcome.Abandon;

            int beats = ctx.BeatsLandedInPhase3;
            int beadsRemaining = ctx.DemonBeads != null ? ctx.DemonBeads.Remaining : 0;

            if (beats == 3) return BattleOutcome.CriticalWin;
            if (beats >= 2) return BattleOutcome.Win;
            if (beadsRemaining == 0) return BattleOutcome.Win;
            return BattleOutcome.Loss;
        }
    }
}
