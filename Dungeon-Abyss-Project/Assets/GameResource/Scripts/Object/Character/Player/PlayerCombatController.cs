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

        protected override void PerformAttack()
        {
            base.PerformAttack();

        }
    }
}