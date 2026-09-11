using UnityEngine;
using R3;
using DdaIT.Object.Management;
using DdaIT.Util;
using DdaIT.Object.Character.Player;

namespace DdaIT.Object.Session
{
    public class HostRunLoop : CachedMonobehaviour{
        private RunSimulation _sim;
        private RunState _state;
        private PlayerSessionActor[] _players;
        private readonly CharacterCommand[] _commands = new CharacterCommand[RunRules.MaxPlayer];
        private byte _localSlot;

        public void Bind(RunSimulation sim, RunState state, PlayerSessionActor[] players, byte localSlot){
            _sim = sim;
            _state = state;
            _players = players;
            _localSlot = localSlot;
        }

        private void FixedUpdate(){
            if(_sim == null || _players == null) return;

            int count = 0;
            for(int i = 0; i < _players.Length; i++){
                var actor = _players[i];
                if(actor == null) continue;

                var cmd = actor.TickLocal(Time.fixedDeltaTime);
                if(actor.Slot.Value != _localSlot) continue;

                _commands[count++] = cmd;
            }

            _sim.Tick(Time.fixedDeltaTime, _commands, count);
        }
    }
}