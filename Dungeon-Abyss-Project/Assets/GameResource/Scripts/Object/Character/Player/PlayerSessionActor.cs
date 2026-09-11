using System;
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

        private RunState _state;
        private IDisposable _damageSub;
        private bool _isLocal;

        public SlotId Slot => new SlotId(_slotValue);

        public void Bind(RunState state, RunEvents events, bool isLocal)
        {
            _state = state;
            _isLocal = isLocal;
            _input.SetListening(isLocal);

            _damageSub?.Dispose();
            _damageSub = events.DamageApplied.Subscribe(OnDamage);
        }

        public CharacterCommand TickLocal(float deltaTime)
        {
            if (_isLocal)
            {
                var snap = _input.Consume();
                _movement.ApplyLocal(snap, deltaTime);

                ref var player = ref _state.Players[Slot.Value];
                player.Position = _movement.SimulationPosition;
                player.Yaw = _movement.SimulationYaw;

                if (snap.AttackPressed && snap.Aim)
                {
                    _combat.TryPlayAttack();
                }

                _animation.Apply(new CharacterPresentation
                {
                    MoveDirection = _movement.MoveDirection,
                    SpeedRatio = _movement.SpeedRatio,
                    IsAiming = _movement.IsAiming,
                }, deltaTime);

                return new CharacterCommand
                {
                    SlotId = Slot,
                    Move = snap.Move,
                    Sprint = snap.Sprint,
                    Aim = snap.Aim,
                    AttackPressed = snap.AttackPressed && snap.Aim,
                    InteractPressed = snap.InteractPressed,
                    Yaw = _movement.SimulationYaw,
                };
            }

            ref var p = ref _state.Players[_slotValue];
            _movement.ApplyReplicated(p.Position, p.Yaw, p.Aiming, p.AnimationParams.SpeedRatio);
            _animation.Apply(new CharacterPresentation
            {
                MoveDirection = _movement.MoveDirection,
                SpeedRatio = _movement.SpeedRatio,
                IsAiming = _movement.IsAiming,
            }, deltaTime);

            return default;
        }

        public void SetViewCamera(Camera camera){
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
