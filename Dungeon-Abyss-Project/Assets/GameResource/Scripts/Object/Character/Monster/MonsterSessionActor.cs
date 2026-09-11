using DdaIT.Object.Session;
using DdaIT.Util;
using Unity.Behavior;
using UnityEngine;

namespace DdaIT.Object.Character.Monster
{
    public class MonsterSessionActor : CachedMonobehaviour
    {
        [SerializeField] private int _monsterIndex;
        [SerializeField] private MonsterDetectController _detect;
        [SerializeField] private BehaviorGraphAgent _agent;

        private RunState _state;
        private RunSimulation _sim;

        public RunSimulation Simulation => _sim;

        public void Bind(RunState state, RunSimulation sim){
            _state = state;
            _sim = sim;

            if(_detect != null){
                
            }
        }
    }
}