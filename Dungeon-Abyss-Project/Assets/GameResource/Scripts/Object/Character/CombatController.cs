using DdaIT.Util.Debug;
using UnityEngine;

namespace DdaIT.Object.Character
{
    public abstract class CombatController : MonoBehaviour
    {
        [SerializeField] private Collider _hitCollider;
        protected Animator _animator;
        protected bool _isAttacking;
        public bool IsHitting => _hitCollider != null && _hitCollider.enabled;
        public bool IsAttacking => _isAttacking;

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

        protected virtual void PerformAttack()
        {
            _isAttacking = true;
            if(_animator != null)
                 _animator.SetTrigger(AttackHash);

            Debugger.LogProgress("Attacking!");
        }

        public void HitStart(){
            if(_hitCollider != null)
                _hitCollider.enabled = true;
        }

        public void HitEnd(){
            if(_hitCollider != null)
                _hitCollider.enabled = false;
        }

        public void OnAttackEnd()
        {
            HitEnd();
            _isAttacking = false;
        }
    }
}
