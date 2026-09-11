namespace DdaIT.Object.Session
{
    public static class RunRules
    {
        public const int MaxPlayer = 4;
        public const float ReviveHpRatio = 0.2f;
        public const float AttackRange = 1.8f;
        public const float AttackCoolDown = 0.6f;
        public const float InteractRange = 1.5f;

        public static bool TeamKill => true;
    }

    /// <summary>
    /// 런 규칙 인터페이스
    /// </summary>
    public interface ISimAuthority
    {
        bool IsHost { get; }
        bool IsOwner(SlotId slot);
    }

    public sealed class SoloHostAuthority : ISimAuthority
    {
        public bool IsHost => true;
        public bool IsOwner(SlotId slot) => true;
    }
}