using UnityEditor;
using UnityEngine;

namespace Backend.Object.Character.Player
{
    public class PlayerAnimationController : AnimationController
    {
        private PlayerMovementController _movementController;

        private readonly int _animSpeed = Animator.StringToHash("Speed");
        private readonly int _animInputX = Animator.StringToHash("InputX");
        private readonly int _animInputY = Animator.StringToHash("InputY");
        private readonly int _animIsAiming = Animator.StringToHash("IsAiming");

        protected override void Awake()
        {
            base.Awake();
            _movementController = GetComponent<PlayerMovementController>();
        }

        private void Update()
        {
            if(_movementController == null)
            {
                return;
            }

            bool isAiming = _movementController.IsAiming;
            SetAnimationBool(_animIsAiming, isAiming);

            Vector3 worldMoveDir = _movementController.MoveDirection;

            bool hasInput = _movementController.InputVector.sqrMagnitude > 0.001f;
            
            float targetSpeed = 0f;

            if (hasInput)
            {
                bool isSprinting = _movementController.IsSprinting;
                targetSpeed = isSprinting ? 1.0f : 0.5f;
            }

            if (isAiming)
            {
                StrafeAnimation(worldMoveDir, targetSpeed);
            }
            else
            {
                LocomotionAnimation(targetSpeed);
            }
        }

        private void LocomotionAnimation(float targetSpeed)
        {
            SetAnimationFloat(_animSpeed, targetSpeed, 0.15f, Time.deltaTime);
            
            SetAnimationFloat(_animInputX, 0f, 0.1f, Time.deltaTime);
            SetAnimationFloat(_animInputY, 0f, 0.1f, Time.deltaTime);
        }

        private void StrafeAnimation(Vector3 worldMoveDir, float currentSpeed)
        {
            Vector3 localMoveDir = transform.InverseTransformDirection(worldMoveDir);
            float inputX = localMoveDir.x;
            float inputY = localMoveDir.z;

            SetAnimationFloat(_animInputX, inputX, 0.1f, Time.deltaTime);
            SetAnimationFloat(_animInputY, inputY, 0.1f, Time.deltaTime);

            SetAnimationFloat(_animSpeed, 0f, 0.1f, Time.deltaTime);
        }
    }
}