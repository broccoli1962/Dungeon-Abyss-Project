using Unity.VisualScripting;
using UnityEngine;

namespace DdaIT.Object.Session
{
    public sealed class RunSimulation
    {
        private readonly RunState _state;
        private readonly RunEvents _events;
        private readonly ISimAuthority _authority;
        private readonly float[] _attackCoolDown = new float[RunRules.MaxPlayer];
        private readonly float[] _downRemaining = new float[RunRules.MaxPlayer];

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
            if (!_authority.IsHost)
                return;

            ClearAttackPulse();

            if(_state.phase != RunPhase.Playing)
                return;

            TickDownTimer(deltaTime);

            if(_state.phase != RunPhase.Playing)
                return;

            for (int i = 0; i < _attackCoolDown.Length; i++)
                _attackCoolDown[i] = Mathf.Max(0f, _attackCoolDown[i] - deltaTime);

            for (int i = 0; i < commandCount; i++)
                ApplyCommand(commands[i]);
        }

        public void ResetPlayerTimer(SlotId slot){
            if(!_authority.IsHost || slot.Value >= _downRemaining.Length)
                return;

            _downRemaining[slot.Value] = 0f;
            _attackCoolDown[slot.Value] = 0f;
        }

        private void SetPlayerLifeState(int index, LifeState next){
            ref var player = ref _state.Players[index];
            if(player.LifeState == next)
                return;

            player.LifeState = next;
            player.AnimationParams.LifeState = next;
            _downRemaining[index] = next == LifeState.Down ? RunRules.BleedOutSeconds : 0f;

            if(next == LifeState.Down || next == LifeState.Dead){
                player.Combat.Hp = 0f;
            }

            if(next != LifeState.Alive){
                player.Aiming = false;
                player.AnimationParams.IsAiming = false;
                player.AnimationParams.MoveDirection = Vector3.zero;
                player.AnimationParams.SpeedRatio = 0f;
                player.AnimationParams.AttackPulse = false;
            }
        }

        private void TickDownTimer(float deltaTime){
            bool someoneDied = false;

            for(int i = 0; i < _state.Players.Length; i++){
                ref var player = ref _state.Players[i];
                if(!player.IsJoined || player.LifeState != LifeState.Down){
                    _downRemaining[i] = 0f;
                    continue;
                }

                _downRemaining[i] = Mathf.Max(0f, _downRemaining[i] - deltaTime);
                if(_downRemaining[i] > 0f){
                    continue;
                }

                SetPlayerLifeState(i, LifeState.Dead);
                someoneDied = true;
            }

            if(someoneDied){
                TryPartyDown();
            }
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
            ref var player = ref _state.Players[i];

            if(!player.IsJoined || player.LifeState != LifeState.Alive)
                return;

            if(!player.Aiming)
                return;

            if(_attackCoolDown[i] > 0f)
                return;

            _attackCoolDown[i] = RunRules.AttackCoolDown;
            player.AnimationParams.AttackPulse = true;
        }

        /// <summary>
        /// 타격 정보로 공격력을 조회하고 최종 데미지를 계산한다. 호스트만 처리한다.
        /// 공격 승인 및 공격별 중복 타격 검증은 호출 전에 수행해야 한다.
        /// </summary>
        public void ResolveHit(in HitTarget attacker, in HitTarget target)
        {
            if (!_authority.IsHost || _state.phase != RunPhase.Playing)
                return;

            float finalDamage;
            switch (attacker.kind)
            {
                case HitTargetKind.Player:
                {
                    if (attacker.Id < 0 || attacker.Id >= _state.Players.Length)
                        return;

                    ref var source = ref _state.Players[attacker.Id];
                    if (!source.IsJoined || source.LifeState != LifeState.Alive)
                        return;

                    finalDamage = source.Combat.Attack;
                    break;
                }
                default:
                    // 몬스터/월드의 전투 상태 조회 경로가 추가되면 확장한다.
                    return;
            }

            ApplyDamage(attacker, target, finalDamage);
        }

        /// <summary>
        /// 서버에서 계산한 최종 데미지를 대상 종류에 맞게 적용한다.
        /// </summary>
        private void ApplyDamage(in HitTarget attacker, in HitTarget target, float finalDamage)
        {
            if (!_authority.IsHost || _state.phase != RunPhase.Playing)
                return;

            if (float.IsNaN(finalDamage) || float.IsInfinity(finalDamage) || finalDamage <= 0f)
                return;

            switch (target.kind)
            {
                case HitTargetKind.Player:
                    break;
                default:
                    // 몬스터/월드의 피해 상태 저장소는 아직 구현되어 있지 않다.
                    return;
            }

            if (target.Id < 0 || target.Id >= _state.Players.Length)
                return;

            ref var player = ref _state.Players[target.Id];
            if (!player.IsJoined || player.LifeState != LifeState.Alive)
                return;

            player.Combat.Hp = Mathf.Max(0f, player.Combat.Hp - finalDamage);
            bool down = player.Combat.Hp <= 0f;
            if (down)
            {
                player.LifeState = LifeState.Down;
                player.Combat.Hp = 0f;
                player.AnimationParams.LifeState = LifeState.Down;
            }

            _events.DamageApplied.OnNext(new DamageApplied(
                attackerSlot: attacker.kind == HitTargetKind.Player ? attacker.Id : -1,
                targetKind: (int)target.kind,
                target.Id,
                finalDamage,
                player.Combat.Hp,
                player.LifeState));

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
            bool hasJoinedPlayer = false;

            for (int i = 0; i < _state.Players.Length; i++)
            {
                ref var player = ref _state.Players[i];
                if(!player.IsJoined)
                    continue;

                hasJoinedPlayer = true;
                if(player.LifeState == LifeState.Alive)
                    return;
            }

            if(!hasJoinedPlayer)
                return;

            for (int i = 0; i < _state.Players.Length; i++){
                if(_state.Players[i].IsJoined && _state.Players[i].LifeState == LifeState.Down){
                    SetPlayerLifeState(i, LifeState.Dead);
                }
            }

            SetPhase(RunPhase.PartyDown);
        }

        private void ClearAttackPulse()
        {
            for (int i = 0; i < _state.Players.Length; i++)
            {
                ref var p = ref _state.Players[i];
                var anim = p.AnimationParams;
                anim.AttackPulse = false;
                p.AnimationParams = anim;
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
