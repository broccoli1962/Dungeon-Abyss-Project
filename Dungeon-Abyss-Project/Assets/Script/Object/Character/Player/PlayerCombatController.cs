using Backend.Util.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Backend.Object.Character.Player
{
    public class PlayerCombatController : CombatController
    {
        private PlayerMovementController _movementController;
        private PlayerInput_Actions _playerInput;
        protected override void Awake()
        {
            base.Awake();

            _movementController = GetComponent<PlayerMovementController>();
            _playerInput = new PlayerInput_Actions();
        }

        private void OnEnable()
        {
            _playerInput.Player.Enable();
            _playerInput.Player.Attack.started += OnAttack;
        }

        private void OnDisable()
        {
            _playerInput.Player.Attack.started -= OnAttack;
            _playerInput.Player.Disable();
        }

        private void OnAttack(InputAction.CallbackContext context)
        {
            if (!_movementController.IsAiming) return;

            OnAttackStart();
        }

        protected override void PerformAttack()
        {
            base.PerformAttack();

        }
    }
}