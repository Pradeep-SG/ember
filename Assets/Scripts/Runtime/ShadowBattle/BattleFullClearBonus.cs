using Kindrith.Dialogue;

namespace Kindrith.ShadowBattle
{
    // Phase 1 full-clear → +10% Phase 2 timer bonus. The bonus applies to the
    // DialogueRunner.OptionTimeoutMs (per-node soft timeout) so the player has
    // ~2s extra to think on each Phase 2 choice when they crushed Phase 1.
    public static class BattleFullClearBonus
    {
        public const int FullClearThresholdMs = 90_000;
        public const float TimerMultiplier = 1.1f;

        // Returns true if Phase 1 ended with beads extinguished AND elapsed < 90s.
        public static bool ShouldApply(BattleContext context)
        {
            if (context == null) return false;
            if (context.DemonBeads == null || !context.DemonBeads.Extinguished) return false;
            return context.Phase1ElapsedMs < FullClearThresholdMs;
        }

        // Mutates the runner's per-option timeout if the bonus applies.
        public static void ApplyTo(BattleContext context, DialogueRunner runner)
        {
            if (runner == null) return;
            if (!ShouldApply(context)) return;
            runner.OptionTimeoutMs = (int)(DialogueRunner.DefaultOptionTimeoutMs * TimerMultiplier);
        }
    }
}
