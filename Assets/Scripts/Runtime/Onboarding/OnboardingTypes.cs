using System;

namespace Kindrith.Onboarding
{
    public enum OnboardingStepId
    {
        WardenName,
        ClassSelect,
        FirstOath,
        FirstChain,
        DemonSummon,
        TutorialBattle,
        Complete,
    }

    [Serializable]
    public sealed class WardenNamePayload
    {
        public string DisplayName;
    }

    [Serializable]
    public sealed class ClassSelectPayload
    {
        public string ClassId; // "scholar" only in v1; spec locks the others
    }

    [Serializable]
    public sealed class FirstOathPayload
    {
        public string Title;
        public string Why;
        public string ClassId;
    }

    [Serializable]
    public sealed class FirstChainPayload
    {
        public string Title;
        public string SeverityHint = "moderate";
    }

    [Serializable]
    public sealed class DemonSummonPayload
    {
        // No fields — DemonSummon is acknowledge-only; the demon record is generated
        // from the Chain payload.
    }

    [Serializable]
    public sealed class TutorialBattlePayload
    {
        // Posted after the tutorial battle completes; carries the BattleRecord id so
        // the flow knows the player actually fought.
        public string BattleId;
    }
}
