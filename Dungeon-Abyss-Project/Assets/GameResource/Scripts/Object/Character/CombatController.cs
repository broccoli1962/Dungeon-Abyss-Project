using System.Collections.Generic;
using DdaIT.Util.Debug;
using R3;
using UnityEngine;

namespace DdaIT.Object.Character
{
    public abstract class CombatController : MonoBehaviour
    {
        [SerializeField] private WeaponHitVolume _weapon;
        
        protected Animator _animator;
        protected bool _isAttacking;
        private bool _isHitting;
        private bool _hitEnabled;

        private readonly Collider[] _buffers = new Collider[8];
        private readonly HashSet<UnityEngine.Object> _hitObjects = new();
        private readonly Subject<Collider> _weaponHit = new();
        private static readonly int AttackHash = Animator.StringToHash("Attack");

        public bool IsAttacking => _isAttacking;
        public Observable<Collider> WeaponHit => _weaponHit;

        protected virtual void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        public bool TryPlayAttack(){
            if(_isAttacking) return false;

            AttackStart();
            return true;
        }

        protected virtual void AttackStart()
        {
            _isAttacking = true;
            if(_animator != null)
                 _animator.SetTrigger(AttackHash);

            Debugger.LogProgress("Attacking!");
        }

        public void HitStart(){
            if(!_isAttacking) return;

            _hitObjects.Clear();
            _isHitting = true;
            OverlapHits();
        }

        public void HitEnd(){
            if(_isHitting)
                OverlapHits();
            _isHitting = false;
        }

        public void AttackEnd()
        {
            HitEnd();
            _isAttacking = false;
        }

        protected virtual void FixedUpdate(){
            if(_isHitting)
                OverlapHits();
        }

        public void CancelAttack(){
            _isHitting = false;
            _isAttacking = false;
            _hitObjects.Clear();

            if(_animator != null)
                _animator.ResetTrigger(AttackHash);
        }

        private void OverlapHits(){
            if(!_hitEnabled || _weapon == null) return;

            int count = Physics.OverlapBoxNonAlloc(
                _weapon.Position, _weapon.HalfExtends, _buffers, _weapon.Rotation, _weapon.TargetMask, QueryTriggerInteraction.Ignore
            );

            for(int i = 0; i < count; i++){
                var hit = _buffers[i];
                UnityEngine.Object key = hit.attachedRigidbody != null ? hit.attachedRigidbody : hit;
                if(_hitObjects.Add(key)){
                    _weaponHit.OnNext(hit);
                }
            }
        }

        public void SetHitQuery(bool enabled) => _hitEnabled = enabled;
    }
}