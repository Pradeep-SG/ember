namespace Kindrith.Core
{
    // Single source of truth for player-facing strings. Lives in Core (not UI as the
    // brief specified) so RewardScreen in ShadowBattle can also consume it without
    // creating a UI ↔ ShadowBattle dependency cycle. New strings added by later WPs
    // land here.
    public static class Strings
    {
        // Home
        public const string AppTitle = "Kindrith";
        public const string ResistButton = "Resist";
        public const string SessionLogEmpty = "No battles yet.";
        public const string SessionLogHeader = "Last battles:";

        // Battle — abandon button
        public const string AbandonButtonLabel = "X";

        // Battle — Phase 3 finisher cues (FinisherCueView)
        public const string FinisherGetReady = "Get ready...";
        public const string FinisherTap = "TAP";
        public const string FinisherHoldSoon = "Hold soon...";
        public const string FinisherKeepHolding = "...keep holding...";
        public const string FinisherHold = "HOLD";
        public const string FinisherRelease = "RELEASE";

        // Reward screen — spec §5 / §6 exact copy. Tests assert these match the spec.
        public const string RewardCriticalWinHeader = "Critical Win.";
        public const string RewardWinHeader = "Win.";
        public const string RewardLossHeader = "The Demon is strong today.";
        public const string RewardLossBody = "You showed up. That is the hardest part. Tomorrow you fight again.";
        public const string RewardAbandonHeader = "Stepped back.";
        public const string RewardAbandonBody = "The battle waits. Come back when you're ready.";
        public const string RewardClarity = "Clarity +1";
        public const string RewardShard = "Shard of Bone";
        public const string RewardReturnPrompt = "[Tap to return to the Realm]";

        // Resume / Abandon sheet (WP-10) — shown on foreground after a sub-60s background.
        public const string ResumeSheetHeader = "Still here.";
        public const string ResumeSheetBody = "The battle paused while you were away.";
        public const string ResumeSheetResume = "Resume";
        public const string ResumeSheetAbandon = "Abandon";
    }
}
