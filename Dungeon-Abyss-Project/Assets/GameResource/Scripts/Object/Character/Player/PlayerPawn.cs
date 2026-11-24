using System.Collections.Generic;
using DdaIT.Object.Session;
using DdaIT.Util;
using UnityEngine;

namespace DdaIT.Object.Character.Player
{
    public struct CharacterPresentation
    {
        public Vector3 MoveDirection;
        public float SpeedRatio;
        public bool IsAiming;
        public bool IsAttacking;
    }

    public sealed class PlayerPawn : CachedMonobehaviour, IHitReceiver
    {
        [SerializeField] private byte _slotValue;
        [SerializeField] private PlayerMovementController _movement;
        [SerializeField] private PlayerCombatController _combat;
        [SerializeField] private PlayerAnimationController _animation;
        [SerializeField] private PlayerPerceptionController _perception;

        private bool _hasLifeState;
        private bool _hasHitAuthority;
        public SlotId Slot => new SlotId(_slotValue);
        public LifeState CurrentLifeState { get; private set; } = LifeState.Absent;
        public bool CanAct => CurrentLifeState == LifeState.Alive;

        /// <summary>입력 스냅샷을 이동과 표현에 반영한다.</summary>
        public void ApplyIntent(in LocalInputSnapShot input, float deltaTime)
        {
            _movement.ApplyLocal(input, deltaTime);
            ApplyPresentation(deltaTime);
        }

        /// <summary>입력이 차단된 틱에는 수평 이동을 멈추고 표현을 갱신한다.</summary>
        public void StopMovement(float deltaTime)
        {
            _movement.StopMovement();
            ApplyPresentation(deltaTime);
        }

        /// <summary>소유자가 계산한 물리 자세와 이동 상태를 읽는다.</summary>
        public PlayerPose ReadPose() => new PlayerPose
        {
            Position = _movement.SimulationPosition,
            Yaw = _movement.SimulationYaw,
            MoveDirection = _movement.MoveDirection,
            IsAiming = _movement.IsAiming,
            SpeedRatio = _movement.SpeedRatio,
        };

        /// <summary>복제된 자세를 이동과 표현에 반영한다.</summary>
        public void ApplyPose(in PlayerPose pose, float deltaTime)
        {
            if (!CanAct)
                _movement.StopMovement();

            _movement.ApplyReplicated(pose.Position, pose.Yaw,
                CanAct ? pose.MoveDirection : Vector3.zero,
                CanAct && pose.IsAiming, CanAct ? pose.SpeedRatio : 0f, deltaTime);
            ApplyPresentation(deltaTime);
        }

        private void ApplyPresentation(float deltaTime)
        {
            _animation.Apply(new CharacterPresentation
            {
                MoveDirection = _movement.MoveDirection,
                SpeedRatio = _movement.SpeedRatio,
                IsAiming = _movement.IsAiming,
                IsAttacking = _combat.IsAttacking,
            }, deltaTime);
            _perception?.SetSimulationPose(_movement.SimulationPosition, _movement.SimulationRotation);
        }

        /// <summary>세션에서 할당한 플레이어 슬롯을 적용한다.</summary>
        public void SetSlot(SlotId slot) => _slotValue = slot.Value;

        /// <summary>생명 상태 전환 시 진행 중인 이동과 공격을 정리한다.</summary>
        public void ApplyLifeState(LifeState state)
        {
            if (_hasLifeState && CurrentLifeState == state)
                return;

            _hasLifeState = true;
            CurrentLifeState = state;
            _combat.CancelAttack();
            _combat.ClearHits();
            _movement.StopMovement();
            _combat.SetHitQuery(_hasHitAuthority && CanAct);
            _animation.ApplyLifeState(state);
        }

        /// <summary>서버에 바인딩된 생존 Pawn만 무기 타격을 수집한다.</summary>
        public void SetHitAuthority(bool isServerBound)
        {
            _hasHitAuthority = isServerBound;
            _combat.SetHitQuery(isServerBound && CanAct);
            if (!isServerBound)
            {
                _combat.CancelAttack();
                _combat.ClearHits();
            }
        }

        /// <summary>승인된 공격을 재생한다.</summary>
        public void PlayAttack()
        {
            if (CanAct)
                _combat.TryPlayAttack();
        }

        /// <summary>생존 중 피격 표현을 재생한다.</summary>
        public void PlayHit()
        {
            if (CanAct)
                _animation.PlayHit();
        }

        /// <summary>서버 시뮬레이션이 처리할 타격을 넘긴다.</summary>
        public void DrainHits(List<HitTarget> hits) => _combat.DrainHit(hits);

        /// <summary>로컬 이동에 사용할 시점 카메라를 연결한다.</summary>
        public void SetViewCamera(Camera camera) => _movement.SetCamera(camera);

        /// <summary>생존 중인 다른 플레이어의 공격에 슬롯 대상을 제공한다.</summary>
        public bool TryHit(Component attacker, out HitTarget target)
        {
            target = default;
            if (!CanAct || (attacker != null && attacker.transform.IsChildOf(transform)))
                return false;

            target = new HitTarget(HitTargetKind.Player, _slotValue);
            return true;
        }
    }
}
