using System;
using System.Collections.Generic;
using DdaIT.Object.Session;
using DdaIT.Util;
using R3;
using UnityEngine;

namespace DdaIT.Object.Character.Player
{
    public struct CharacterPresentation
    {
        public Vector3 MoveDirection;
        public float SpeedRatio;
        public bool IsAiming;
    }

    public class PlayerSessionActor : CachedMonobehaviour, IHitReceiver
    {
        [SerializeField] private Byte _slotValue;
        [SerializeField] private LocalPlayerInput _input;
        [SerializeField] private PlayerMovementController _movement;
        [SerializeField] private PlayerCombatController _combat;
        [SerializeField] private PlayerAnimationController _animation;
        [SerializeField] private PlayerPerceptionController _perception;
        [SerializeField] private PlayerNetwork _network;

        private IDisposable _damageSub;
        private IDisposable _hitSub;
        private readonly List<HitTarget> _pendingHits = new();
        private bool _hasInputAuthority;
        private bool _gameplayInputEnabled = true;
        private bool _isServerBound;
        private bool _hasLifeState;
        private LifeState _lifeState = LifeState.Absent;

        public PerceptionController Perception => _perception;
        public SlotId Slot => new SlotId(_slotValue);
        public LifeState CurrentLifeState => _lifeState;
        public bool CanAct => _lifeState == LifeState.Alive;

        public void Bind(RunEvents events)
        {
            if (_network == null || !_network.IsServer)
                return;

            UnBind();
            _isServerBound = true;
            _damageSub = events.DamageApplied.Subscribe(OnDamage);
            _hitSub = _combat.WeaponHit.Subscribe(OnWeaponHit);
            _combat.SetHitQuery(CanAct);
        }

        public void UnBind()
        {
            _damageSub?.Dispose();
            _damageSub = null;
            _hitSub?.Dispose();
            _hitSub = null;
            _isServerBound = false;
            _pendingHits.Clear();
            _combat.SetHitQuery(false);
            _combat.CancelAttack();
        }

        public void SetInputAuthority(bool hasAuthority)
        {
            _hasInputAuthority = hasAuthority;
            RefreshInputListening();
        }

        /// <summary>컷씬 등 외부 상황에 따라 로컬 게임 입력을 허용하거나 차단한다.</summary>
        public void SetGameplayInputEnabled(bool enabled)
        {
            _gameplayInputEnabled = enabled;
            RefreshInputListening();
        }

        private void RefreshInputListening()
        {
            _input.SetListening(_hasInputAuthority && _gameplayInputEnabled && CanAct);
        }

        public CharacterCommand TickLocal(float deltaTime)
        {
            bool canReadInput = _hasInputAuthority && _gameplayInputEnabled && CanAct;

            LocalInputSnapShot snap = default;
            if (canReadInput)
            {
                snap = _input.Consume();
                _movement.ApplyLocal(snap, deltaTime);
            }
            else
            {
                _movement.StopMovement();
            }

            ApplyPresentation(deltaTime);

            return new CharacterCommand
            {
                SlotId = Slot,
                Position = _movement.SimulationPosition,
                MoveDirection = _movement.MoveDirection,
                Sprint = snap.Sprint,
                Aim = snap.Aim,
                AttackPressed = snap.AttackPressed && snap.Aim,
                InteractPressed = snap.InteractPressed,
                Yaw = _movement.SimulationYaw,
                SpeedRatio = _movement.SpeedRatio,
            };
        }

        private void ApplyPresentation(float deltaTime)
        {
            _animation.Apply(new CharacterPresentation
            {
                MoveDirection = _movement.MoveDirection,
                SpeedRatio = _movement.SpeedRatio,
                IsAiming = _movement.IsAiming,
            }, deltaTime);

            PushSimulationPoseFromMovement();
        }

        public void ApplyLifeState(LifeState state)
        {
            if (_hasLifeState && _lifeState == state)
            {
                return;
            }

            _hasLifeState = true;
            _lifeState = state;

            _combat.CancelAttack();
            _pendingHits.Clear();
            _movement.StopMovement();
            _combat.SetHitQuery(_isServerBound && CanAct);

            RefreshInputListening();
            _animation.ApplyLifeState(state);
        }

        public void ApplyRemote(in PlayerPose pose, float deltaTime)
        {
            if (!CanAct)
                _movement.StopMovement();

            _movement.ApplyReplicated(
                pose.Position,
                pose.Yaw,
                CanAct ? pose.MoveDirection : Vector3.zero,
                CanAct && pose.IsAiming,
                CanAct ? pose.SpeedRatio : 0f,
                deltaTime);

            ApplyPresentation(deltaTime);
        }

        public void PlayRemoteAttack(bool canDetectHits)
        {
            if (!CanAct)
                return;

            _combat.SetHitQuery(canDetectHits && _isServerBound);
            _combat.TryPlayAttack();
        }

        public void PlayRemoteHit()
        {
            if (CanAct)
                _animation.PlayHit();
        }

        public void PushSimulationPoseFromMovement()
        {
            if (_perception == null || _movement == null)
                return;

            _perception.SetSimulationPose(_movement.SimulationPosition, _movement.SimulationRotation);
        }

        public void ApplySimulationPose(Vector3 position, float yawDegrees)
        {
            _perception?.SetSimulationPose(position, yawDegrees);
        }

        public void SetViewCamera(Camera camera)
        {
            _movement.SetCamera(camera);
        }

        public void SetSlot(SlotId slotId)
        {
            _slotValue = slotId.Value;
        }

        private void OnDamage(DamageApplied e)
        {
            if (!_isServerBound || _network == null || !_network.IsServer)
            {
                return;
            }

            if (e.TargetKind != (int)HitTargetKind.Player || e.TargetId != _slotValue)
            {
                return;
            }

            _network.ServerNotifyDamage(e);
        }

        //테스트 함수
        public void DrainHit(List<HitTarget> hits)
        {
            if (_isServerBound && CanAct)
            {
                hits.AddRange(_pendingHits);
            }
            _pendingHits.Clear();
        }

        private void OnWeaponHit(Collider other)
        {
            if(_isServerBound && !CanAct || other == null)
                return;

            if (!other.TryGetComponent<IHitReceiver>(out var receiver))
                return;

            if (!receiver.TryHit(this, out var hit))
                return;

            _pendingHits.Add(hit);
        }

        private void OnDestroy()
        {
            _damageSub?.Dispose();
            _hitSub?.Dispose();
        }

        public bool TryHit(Component attacker, out HitTarget target)
        {
            target = default;
            if(!CanAct){
                return false;
            }

            if (attacker != null && attacker.transform.IsChildOf(transform))
            {
                return false;
            }
            
            target = new HitTarget(HitTargetKind.Player, _slotValue);
            return true;
        }
    }
}
