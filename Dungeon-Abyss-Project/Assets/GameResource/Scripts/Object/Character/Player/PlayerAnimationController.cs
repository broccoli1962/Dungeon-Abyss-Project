using DdaIT.Object.Session;
using UnityEngine;

namespace DdaIT.Object.Character.Player
{
    public class PlayerAnimationController : AnimationController
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int InputXHash = Animator.StringToHash("InputX");
        private static readonly int InputYHash = Animator.StringToHash("InputY");
        private static readonly int AimHash = Animator.StringToHash("IsAiming");
        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int LifeHash = Animator.StringToHash("LifeState");

        private static readonly int LocomotionState = Animator.StringToHash("Base Layer.Locomotion");
        private static readonly int DownState = Animator.StringToHash("Base Layer.Down");
        private static readonly int DeadState = Animator.StringToHash("Base Layer.Dead");
        private static readonly int EmptyAttackState = Animator.StringToHash("Attack.Empty");

        private LifeState _lifeState = LifeState.Absent;
        private bool _hasLifeState;
        private int _attackLayer = -1;
        private float _attackWeight;
        private const float ATTACK_RELEASE_SECONDS = 0.1f;

        protected override void Awake()
        {
            base.Awake();
            _attackLayer = _animator.GetLayerIndex("Attack");
            if(_attackLayer >= 0){
                _attackWeight = _animator.GetLayerWeight(_attackLayer);
                _animator.SetLayerWeight(_attackLayer, 0f);
            }
        }

        public void ApplyLifeState(LifeState state){
            if(_hasLifeState && _lifeState == state)
                return;

            _hasLifeState = true;
            _lifeState = state;
            SetAnimationInt(LifeHash, (int)state);
            _animator.ResetTrigger(HitHash);
            _animator.ResetTrigger(AttackHash);
            ResetLocomotionParameters();

            if(_attackLayer >= 0){
                _animator.Play(EmptyAttackState, _attackLayer, 0f);
                _animator.SetLayerWeight(_attackLayer, 0f);
            }

            switch(state){
                case LifeState.Alive:
                    _animator.CrossFadeInFixedTime(LocomotionState, 0.08f, 0, 0f);
                    break;
                case LifeState.Down:
                    _animator.CrossFadeInFixedTime(DownState, 0.08f, 0, 0f);
                    break;
                case LifeState.Dead:
                    _animator.CrossFadeInFixedTime(DeadState, 0.08f, 0, 0f);
                    break;
            }
        }

        public void Apply(in CharacterPresentation p, float dt)
        {
            if(_lifeState != LifeState.Alive){
                ResetLocomotionParameters();
                return;
            }

            SetAnimationBool(AimHash, p.IsAiming);
            if(_attackLayer >= 0){
                float weight = p.IsAttacking ? _attackWeight : Mathf.MoveTowards(
                    _animator.GetLayerWeight(_attackLayer), 0f, _attackWeight * dt / ATTACK_RELEASE_SECONDS);
                _animator.SetLayerWeight(_attackLayer, weight);
            }
            if (p.IsAiming)
            {
                var local = transform.InverseTransformDirection(p.MoveDirection);
                SetAnimationFloat(InputXHash, local.x, 0.1f, dt);
                SetAnimationFloat(InputYHash, local.z, 0.1f, dt);
                SetAnimationFloat(SpeedHash, 0f, 0.1f, dt);
            }
            else
            {
                SetAnimationFloat(SpeedHash, p.SpeedRatio, 0.15f, dt);
                SetAnimationFloat(InputXHash, 0f, 0.1f, dt);
                SetAnimationFloat(InputYHash, 0f, 0.1f, dt);
            }
        }

        public void PlayHit(){
            if(_lifeState != LifeState.Alive){
                return;
            }

            SetAnimationTrigger(HitHash);
        }

        private void ResetLocomotionParameters(){
            SetAnimationBool(AimHash, false);
            SetAnimationFloat(InputXHash, 0f);
            SetAnimationFloat(InputYHash, 0f);
            SetAnimationFloat(SpeedHash, 0f);
        }
    }
}
