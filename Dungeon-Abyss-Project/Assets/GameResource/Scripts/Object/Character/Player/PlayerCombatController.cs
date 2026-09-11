using DdaIT.Util.Input;
using UnityEngine.InputSystem;

namespace DdaIT.Object.Character.Player
{
    public class PlayerCombatController : CombatController
    {
        private PlayerMovementController _movementController;
        protected override void Awake()
        {
            base.Awake();

            _movementController = GetComponent<PlayerMovementController>();
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