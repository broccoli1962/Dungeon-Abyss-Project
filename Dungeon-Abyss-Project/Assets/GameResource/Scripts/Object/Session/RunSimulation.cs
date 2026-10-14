using UnityEngine;

namespace DdaIT.Object.Session
{
    public sealed class RunSimulation
    {
        private readonly RunState _state;
        private readonly RunEvents _events;
        private readonly ISimAuthority _authority;
        private readonly float[] _attackCoolDown = new float[RunRules.MaxPlayer];

        public RunSimulation(RunState state, RunEvents events, ISimAuthority authority)
        {
            _state = state;
            _events = events;
            _authority = authority;
        }

        /// <summary>
        /// 호스트만 호출
        /// </summary>
        public void Tick(float deltaTime, CharacterCommand[] commands, int commandCount)
        {
            if (!_authority.IsHost || _state.phase != RunPhase.Playing)
                return;

            ClearAttackPulse();

            for (int i = 0; i < commandCount; i++)
                _attackCoolDown[i] = Mathf.Max(0f, _attackCoolDown[i] - deltaTime);

            for (int i = 0; i < commandCount; i++)
                ApplyCommand(commands[i]);
        }

        private void ApplyCommand(in CharacterCommand cmd)
        {
            ref var player = ref _state.Players[cmd.SlotId.Value];
            if (!player.IsJoined || player.LifeState != LifeState.Alive)
                return;

            player.Position = cmd.Position;
            player.AnimationParams.MoveDirection = cmd.MoveDirection;
            player.Yaw = cmd.Yaw;
            player.Aiming = cmd.Aim;
            player.AnimationParams.IsAiming = cmd.Aim;
            player.AnimationParams.SpeedRatio = cmd.SpeedRatio;

            if (cmd.AttackPressed)
                TryAttack(cmd.SlotId);

            if (cmd.InteractPressed)
                TryInteract(cmd.SlotId);
        }

        private void TryAttack(SlotId slot)
        {
            int i = slot.Value;
            if (_attackCoolDown[i] > 0f)
                return;

            _attackCoolDown[i] = RunRules.AttackCoolDown;
            _state.Players[i].AnimationParams.AttackPulse = true;
        }

        /// <summary>
        /// 플레이어에게 데미지를 적용한다. 호스트만 호출한다.
        /// </summary>
        public void ApplyDamageToPlayer(SlotId slot, float damage)
        {
            if (!_authority.IsHost)
                return;

            ref var player = ref _state.Players[slot.Value];
            if (player.LifeState != LifeState.Alive)
                return;

            player.Combat.Hp = Mathf.Max(0f, player.Combat.Hp - damage);
            bool down = player.Combat.Hp <= 0f;
            if (down)
            {
                player.LifeState = LifeState.Down;
                player.Combat.Hp = 0f;
                player.AnimationParams.LifeState = LifeState.Down;
            }

            _events.DamageApplied.OnNext(new DamageApplied(
                attackerSlot: -1,
                targetKind: 0,
                slot.Value,
                damage,
                player.Combat.Hp,
                down));

            if (down)
                TryPartyDown();
        }

        private void TryInteract(SlotId slot)
        {
            ref var player = ref _state.Players[slot.Value];

            for (int i = 0; i < _state.Doors.Length; i++)
            {
                ref var door = ref _state.Doors[i];
                if (door.IsOpen)
                    continue;
                if (door.Locked && !player.HasKey)
                    continue;

                door.Locked = false;
                door.IsOpen = true;
                _events.DoorOpened.OnNext(new DoorOpened(door.DoorId, slot));
                return;
            }
        }

        private void TryPartyDown()
        {
            for (int i = 0; i < _state.Players.Length; i++)
            {
                if (_state.Players[i].IsJoined && _state.Players[i].LifeState == LifeState.Alive)
                    return;
            }

            SetPhase(RunPhase.PartyDown);
        }

        private void ClearAttackPulse()
        {
            for (int i = 0; i < _state.Players.Length; i++)
            {
                _state.Players[i].AnimationParams.AttackPulse = false;
            }
        }

        private void SetPhase(RunPhase phase)
        {
            if (_state.phase == phase)
                return;
            _state.phase = phase;
            _events.PhaseChanged.OnNext(phase);
        }

        /// <summary>
        /// 런을 시작한다. 호스트만 호출한다.
        /// </summary>
        public void StartRun(uint seed)
        {
            if (!_authority.IsHost)
                return;

            _state.RunSeed = seed;
            _state.FoorIndex = 0;
            SetPhase(RunPhase.Playing);
        }
    }
}
