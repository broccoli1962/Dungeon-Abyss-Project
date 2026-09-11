using DdaIT.Object.Session;
using UnityEngine;

namespace DdaIT.Object.Character.Player
{
    public class PlayerAnimationController : AnimationController
    {
        private readonly int _animSpeed = Animator.StringToHash("Speed");
        private readonly int _animInputX = Animator.StringToHash("InputX");
        private readonly int _animInputY = Animator.StringToHash("InputY");
        private readonly int _animIsAiming = Animator.StringToHash("IsAiming");

        public void Apply(in CharacterPresentation p, float dt)
        {
            SetAnimationBool(_animIsAiming, p.IsAiming);
            if (p.IsAiming)
            {
                var local = transform.InverseTransformDirection(p.MoveDirection);
                SetAnimationFloat(_animInputX, local.x, 0.1f, dt);
                SetAnimationFloat(_animInputY, local.z, 0.1f, dt);
                SetAnimationFloat(_animSpeed, 0f, 0.1f, dt);
            }
            else
            {
                SetAnimationFloat(_animSpeed, p.SpeedRatio, 0.15f, dt);
                SetAnimationFloat(_animInputX, 0f, 0.1f, dt);
                SetAnimationFloat(_animInputY, 0f, 0.1f, dt);
            }
        }
    }
}