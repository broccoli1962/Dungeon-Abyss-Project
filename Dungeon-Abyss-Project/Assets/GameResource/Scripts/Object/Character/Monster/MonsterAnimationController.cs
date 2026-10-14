using UnityEngine;

namespace DdaIT.Object.Character
{
    public class MonsterAnimationController : AnimationController
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int DieHash = Animator.StringToHash("Die");

        public void SetMoveSpeed(float ratio)
        {
            if (_animator == null)
                return;

            SetAnimationFloat(SpeedHash, ratio);
        }

        public void PlayDeath()
        {
            if (_animator == null)
                return;

            SetAnimationTrigger(DieHash);
        }
    }
}
