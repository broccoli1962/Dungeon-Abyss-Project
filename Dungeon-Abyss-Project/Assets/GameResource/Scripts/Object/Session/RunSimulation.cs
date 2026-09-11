using System.Runtime.InteropServices.WindowsRuntime;
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

            for (int i = 0; i < commandCount; i++)
                _attackCoolDown[i] = Mathf.Max(0f, _attackCoolDown[i] - deltaTime);

            for (int i = 0; i < commandCount; i++)
                ApplyCommand(commands[i]);

            TickMonster(deltaTime);
        }

        private void ApplyCommand(in CharacterCommand cmd)
        {
            ref var player = ref _state.Players[cmd.SlotId.Value];
            if (!player.IsJoined || player.LifeState != LifeState.Alive)
                return;

            player.Yaw = cmd.Yaw;
            player.Aiming = cmd.Aim;
            player.AnimationParams.IsAiming = cmd.Aim;
            player.AnimationParams.SpeedRatio = cmd.Move.sqrMagnitude > 0.01f ? (cmd.Sprint ? 1f : 0.5f) : 0f;

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

            ref var attacker = ref _state.Players[i];
            for (int j = 0; j < _state.Monsters.Length; j++)
            {
                ref var monster = ref _state.Monsters[j];
                if (!monster.IsAlive)
                    continue;
                if (Vector3.Distance(attacker.Position, monster.Position) > RunRules.AttackRange)
                    continue;

                ApplyDamageToMonster(j, attacker.Slot.Value, attacker.Combat.Attack);
            }
        }

        private void ApplyDamageToMonster(int monsterId, int attackerSlot, float damage)
        {
            ref var monster = ref _state.Monsters[monsterId];
            if (!monster.IsAlive)
                return;

            monster.Combat.Hp = Mathf.Max(0f, monster.Combat.Hp - damage);
            bool dead = monster.Combat.Hp <= 0f;
            if (dead)
            {
                monster.IsAlive = false;
            }

            _events.DamageApplied.OnNext(new DamageApplied(attackerSlot, targetKind: 1, monster.MonsterId, damage, monster.Combat.Hp, dead));
        }

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

        private void TryRevive(SlotId actor, SlotId target)
        {
            if (!_authority.IsHost)
                return;

            ref var a = ref _state.Players[actor.Value];
            ref var t = ref _state.Players[target.Value];
            if (a.LifeState != LifeState.Alive || t.LifeState != LifeState.Down)
            {
                return;
            }
            if (Vector3.Distance(a.Position, t.Position) > RunRules.InteractRange)
            {
                return;
            }

            t.LifeState = LifeState.Alive;
            t.Combat.Hp = t.Combat.MaxHp * RunRules.ReviveHpRatio;
            t.AnimationParams.LifeState = LifeState.Alive;
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

        private void TickMonster(float deltaTime)
        {

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

        private void ClearAttackPulse(){
            for(int i = 0; i < _state.Players.Length; i++){
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