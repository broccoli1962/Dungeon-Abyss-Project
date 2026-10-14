using DdaIT.Object.Character;
using DdaIT.Util;
using Unity.Behavior;
using UnityEngine;

namespace DdaIT.Object.Character.Monster
{
    public class MonsterSessionActor : CachedMonobehaviour
    {
        [SerializeField] private BehaviorGraphAgent _agentTree;
        [SerializeField] private MonsterMovementController _movement;
        [SerializeField] private MonsterCombatController _combat;
        [SerializeField] private MonsterAnimationController _animation;
        [SerializeField] private MonsterPerceptionController _perception;

        public MonsterMovementController Movement => _movement;
        public MonsterCombatController Combat => _combat;
        public MonsterAnimationController Animation => _animation;
        public PerceptionController Perception => _perception;

        public void Bind()
        {

        }

        private void FixedUpdate()
        {
            PushSimulationPoseFromMovement();
        }

        /// <summary>호스트: Movement(BT) sim 직후. 클라: replicate 적용 직후에도 동일 호출.</summary>
        public void PushSimulationPoseFromMovement()
        {
            if (_perception == null || _movement == null)
                return;

            _perception.SetSimulationPose(_movement.SimulationPosition, _movement.SimulationRotation);
        }

        /// <summary>RunState 등 권위 pose (호스트 sim → 스냅샷 → 클라 Apply).</summary>
        public void ApplySimulationPose(Vector3 position, float yawDegrees)
        {
            _perception?.SetSimulationPose(position, yawDegrees);
        }
    }
}
