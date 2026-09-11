using UnityEngine;

namespace DdaIT.Object.Session
{
    public struct CharacterCommand
    {
        public SlotId SlotId;
        public Vector2 Move;
        public bool Sprint;
        public bool Aim;
        public bool AttackPressed;
        public bool InteractPressed;
        public float Yaw;
    }

    /// <summary>
    /// 애니메이션 상태 정보
    /// </summary>
    public struct CharacterAnimationParams{
        public float SpeedRatio;
        public bool IsAiming;
        public bool AttackPulse;
        public LifeState LifeState;
    }
}
