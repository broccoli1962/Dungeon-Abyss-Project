using System.Collections.Generic;
using DdaIT.Object.Character;
using DdaIT.Object.Character.Player;
using DdaIT.Util;
using UnityEngine;

namespace DdaIT.Object.Session
{
    public class HostRunLoop : CachedMonobehaviour
    {
        private RunSimulation _sim;
        private RunState _state;
        private PlayerSessionActor[] _players;
        private readonly CharacterCommand[] _commands = new CharacterCommand[RunRules.MaxPlayer];
        private readonly List<HitTarget> _hitBuffer = new();
        private byte _localSlot;

        public void Bind(RunSimulation sim, RunState state, PlayerSessionActor[] players, byte localSlot)
        {
            _sim = sim;
            _state = state;
            _players = players;
            _localSlot = localSlot;
        }

        private void FixedUpdate()
        {
            if (_sim == null || _players == null) return;

            int count = 0;
            for (int i = 0; i < _players.Length; i++)
            {
                var actor = _players[i];
                if (actor == null)
                    continue;

                if (!actor.TryGetComponent<PlayerNetwork>(out var playerNet))
                    continue;

                if (playerNet.TryConsumeCommand(out var cmd))
                    _commands[count++] = cmd;
            }

            _sim.Tick(Time.fixedDeltaTime, _commands, count);

            for (int i = 0; i < _players.Length; i++)
            {
                var actor = _players[i];
                if (actor == null)
                    continue;

                _hitBuffer.Clear();
                actor.DrainHit(_hitBuffer);

                for (int h = 0; h < _hitBuffer.Count; h++)
                {
                    _sim.ResolveHit(new HitTarget(HitTargetKind.Player, actor.Slot.Value), _hitBuffer[h]);
                }
            }

            for (int i = 0; i < _players.Length; i++)
            {
                var actor = _players[i];
                if (actor == null)
                    continue;

                if (!actor.TryGetComponent<PlayerNetwork>(out var playerNet))
                    continue;

                var player = _state.Players[actor.Slot.Value];
                playerNet.ServerSetLifeState(player.LifeState);
                playerNet.ServerSetPose(new PlayerPose
                {
                    Position = player.Position,
                    Yaw = player.Yaw,
                    MoveDirection = player.AnimationParams.MoveDirection,
                    SpeedRatio = player.AnimationParams.SpeedRatio,
                    IsAiming = player.Aiming,
                });

                playerNet.ServerSetVitals(new PlayerVital
                {
                    Health = player.Combat.Hp,
                    MaxHealth = player.Combat.MaxHp,
                    Stamina = player.Stamina,
                    MaxStamina = player.MaxStamina,
                    Hunger = player.Hunger,
                    MaxHunger = player.MaxHunger,
                });

                if (_state.phase == RunPhase.Playing
                    && player.LifeState == LifeState.Alive
                    && player.AnimationParams.AttackPulse)
                {
                    playerNet.ServerPlayAttack();
                }
            }
        }
    }
}