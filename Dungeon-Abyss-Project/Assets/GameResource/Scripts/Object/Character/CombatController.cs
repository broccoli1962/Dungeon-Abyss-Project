using Backend.Util.Debug;
using UnityEngine;

namespace Backend.Object.Character
{
    public abstract class CombatController : MonoBehaviour
    {
        [Header("Combat Setting")]
        [SerializeField] protected float _attackRange;

        protected Status _status;
        protected Animator _animator;

        protected bool _isAttacking;

        protected virtual void Awake()
        {
            _status = GetComponent<Status>();
            _animator = GetComponent<Animator>();
        }

        public void OnAttackStart()
        {
            PerformAttack();
        }

        protected virtual void PerformAttack()
        {
            _isAttacking = true;

            if(_animator != null)
            {
                _animator.SetTrigger("Attack");
            }

            Debugger.LogProgress("Attacking!");
        }

        public void OnAttackEnd()
        {
            _isAttacking = false;
        }
    }
}