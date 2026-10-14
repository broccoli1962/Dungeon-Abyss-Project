using System;
using DdaIT.Object.Character;
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

    public class PlayerSessionActor : CachedMonobehaviour
    {
        [SerializeField] private Byte _slotValue;
        [SerializeField] private LocalPlayerInput _input;
        [SerializeField] private PlayerMovementController _movement;
        [SerializeField] private PlayerCombatController _combat;
        [SerializeField] private PlayerAnimationController _animation;
        [SerializeField] private PlayerPerceptionController _perception;

        public PerceptionController Perception => _perception;

        private IDisposable _damageSub;
        private bool _isLocal;
        private bool ReadsInput
        {
            get
            {
                if (_isLocal) return true;
                return TryGetComponent<PlayerNetwork>(out var playerNet) && playerNet.IsOwner;
            }
        }

        public SlotId Slot => new SlotId(_slotValue);

        public void Bind(RunEvents events, bool isLocal)
        {
            _isLocal = isLocal;
            _input.SetListening(isLocal);

            _damageSub?.Dispose();
            _damageSub = events.DamageApplied.Subscribe(OnDamage);
        }

        public CharacterCommand TickLocal(float deltaTime)
        {
            if (!ReadsInput)
                return default;

            if (!_input.IsListening)
                _input.SetListening(true);

            var snap = _input.Consume();
            _movement.ApplyLocal(snap, deltaTime);

            if (snap.AttackPressed && snap.Aim)
            {
                _combat.TryPlayAttack();
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

        private void ApplyPresentation(float deltaTime){
            _animation.Apply(new CharacterPresentation{
                MoveDirection = _movement.MoveDirection,
                SpeedRatio = _movement.SpeedRatio,
                IsAiming = _movement.IsAiming,
            }, deltaTime);

            PushSimulationPoseFromMovement();
        }

        public void ApplyRemote(in PlayerPose pose, float deltaTime){
            _movement.ApplyReplicated(pose.Position, pose.Yaw, pose.MoveDirection, pose.IsAiming, pose.SpeedRatio, deltaTime);
            ApplyPresentation(deltaTime);
        }

        public void PlayRemoteAttack(){
            _combat.TryPlayAttack();
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
            if (e.TargetKind != 0 || e.TargetId != _slotValue) return;
        }

        private void OnDestroy()
        {
            _damageSub?.Dispose();
        }
    }
}
