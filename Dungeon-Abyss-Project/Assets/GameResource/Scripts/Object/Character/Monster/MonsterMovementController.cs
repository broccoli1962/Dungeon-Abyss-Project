using DdaIT.Object.Character;
using UnityEngine;

namespace DdaIT.Object.Character
{
    public class MonsterMovementController : MovementController
    {
        [Header("Move Setting")]
        [SerializeField] private float _walkSpeed = 2f;
        [SerializeField] private float _runSpeed = 4f;
        [SerializeField] private float _rotSpeed = 4f;

        public void WalkTo(Vector3 targetPos){
            Vector3 direction = GetDirection(targetPos);
            _rigidbody.MovePosition(SimulationPosition + direction * _walkSpeed * Time.deltaTime);
        }

        public void RunTo(Vector3 targetPos){
            Vector3 direction = GetDirection(targetPos);
            _rigidbody.MovePosition(SimulationPosition + direction * _runSpeed * Time.deltaTime);
        }

        public void StopMove(){
            _rigidbody.angularVelocity = Vector3.zero;
        }

        public void RotateTo(Vector3 targetPos){
            Vector3 direction = GetDirection(targetPos);
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            _rigidbody.MoveRotation(Quaternion.RotateTowards(SimulationRotation, targetRotation, _rotSpeed * Time.deltaTime));
        }
    }
}
