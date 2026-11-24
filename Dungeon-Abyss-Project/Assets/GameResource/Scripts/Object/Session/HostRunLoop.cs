using System.Collections.Generic;
using DdaIT.Object.Character;
using DdaIT.Util;
using UnityEngine;

namespace DdaIT.Object.Session
{
    public class HostRunLoop : CachedMonobehaviour
    {
        private RunSimulation _sim;
        private RunState _state;
        private PlayerNetwork[] _players;
        private readonly CharacterCommand[] _commands = new CharacterCommand[RunRules.MaxPlayer];
        private readonly List<HitTarget> _hitBuffer = new();

        /// <summary>서버 시뮬레이션과 접속 슬롯별 네트워크 객체를 연결한다.</summary>
        public void Bind(RunSimulation sim, RunState state, PlayerNetwork[] players)
        {
            _sim = sim;
            _state = state;
            _players = players;
        }

        private void FixedUpdate()
        {
            if (_sim == null || _players == null)
                return;

            int count = 0;
            foreach (var player in _players)
            {
                if (player != null && player.TryConsumeCommand(out var cmd))
                    _commands[count++] = cmd;
            }

            _sim.Tick(Time.fixedDeltaTime, _commands, count);

            foreach (var player in _players)
            {
                if (player == null)
                    continue;

                _hitBuffer.Clear();
                player.DrainHits(_hitBuffer);
                var attacker = new HitTarget(HitTargetKind.Player, player.Slot.Value);
                foreach (var hit in _hitBuffer)
                    _sim.ResolveHit(attacker, hit);
            }

            foreach (var player in _players)
            {
                if (player == null)
                    continue;

                var state = _state.Players[player.Slot.Value];
                player.ServerPublish(state);
                if (_state.phase == RunPhase.Playing && state.LifeState == LifeState.Alive && state.AnimationParams.AttackPulse)
                    player.ServerPlayAttack();
            }
        }
    }
}
