using DdaIT.Util.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DdaIT.Object.Character.Player
{
    public class PlayerCombatController : CombatController
    {
        protected override void Awake()
        {
            base.Awake();
        }

        protected override void AttackStart()
        {
            base.AttackStart();
        }

        // public bool TryResolveHit(Collider other, Component attacker, out HitTarget target){

        //     return false;
        // }
    }
}