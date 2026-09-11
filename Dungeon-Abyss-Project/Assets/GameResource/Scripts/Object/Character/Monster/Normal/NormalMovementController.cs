using UnityEngine.AI;
using UnityEngine;

namespace DdaIT.Object.Character.Monster.Normal
{
    public class NormalMovementController : MovementController
    {
        private NavMeshPath _path;
        private int _currentCornerIndex;
        private Vector3 _lastTargetPos;

        protected override void Awake()
        {
            base.Awake();
            _path = new NavMeshPath();
        }

        public void MoveToDestination(Vector3 targetPos, float speed, float rotSpeed)
        {
            if(GetDistance(_lastTargetPos, targetPos) > 0.5f)
            {
                _lastTargetPos = targetPos;
                if(NavMesh.CalculatePath(SimulationPosition, targetPos, NavMesh.AllAreas, _path))
                {
                    _currentCornerIndex = 1;
                }
            }

            if (_path.status == NavMeshPathStatus.PathInvalid || _path.corners.Length < 2) return;

            if(_currentCornerIndex < _path.corners.Length)
            {
                Vector3 targetWayPoint = _path.corners[_currentCornerIndex];
                
                float distToCorner = Vector3.Distance(new Vector3(SimulationPosition.x, 0, SimulationPosition.z), new Vector3(targetWayPoint.x, 0, targetWayPoint.z));
                if(distToCorner < 0.3f)
                {
                    _currentCornerIndex++;
                }

                PhysicsMove(targetWayPoint, speed, rotSpeed);
            }
        }

        private void PhysicsMove(Vector3 targetPos, float speed, float rotSpeed)
        {
            Vector3 moveDir = GetDirection(targetPos);

            Vector3 velocity = moveDir * speed;
            velocity.y = _rigidbody.linearVelocity.y;
            _rigidbody.linearVelocity = velocity;

            if(moveDir != Vector3.zero)
            {
                Quaternion lookRot = Quaternion.LookRotation(moveDir);
                _rigidbody.MoveRotation(Quaternion.Slerp(SimulationRotation, lookRot, Time.deltaTime * rotSpeed));
            }
        }

        public void Stop()
        {
            _rigidbody.linearVelocity = new Vector3(0, _rigidbody.linearVelocity.y, 0);
            _lastTargetPos = Vector3.zero;
        }
    }
}
