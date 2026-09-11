using DdaIT.Util.Debug;
using UnityEngine;

namespace DdaIT.Object.Character
{
    public abstract class CombatController : MonoBehaviour
    {
        [Header("Combat Setting")]
        [SerializeField] protected float _attackRange;

        protected Animator _animator;
        protected bool _isAttacking;

        private static readonly int AttackHash = Animator.StringToHash("Attack");

        protected virtual void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        public bool TryPlayAttack(){
            if(_isAttacking) return false;

            PerformAttack();
            return true;
        }

        public void OnAttackStart() => TryPlayAttack();

        protected virtual void PerformAttack()
        {
            _isAttacking = true;
            if(_animator != null)
                 _animator.SetTrigger(AttackHash);

            Debugger.LogProgress("Attacking!");
        }

        public void OnAttackEnd()
        {
            _isAttacking = false;
        }
    }
}