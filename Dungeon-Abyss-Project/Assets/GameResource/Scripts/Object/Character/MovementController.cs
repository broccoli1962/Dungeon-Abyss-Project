using DdaIT.Util;
using UnityEngine;

namespace DdaIT.Object.Character
{
    [RequireComponent(typeof(Rigidbody))]
    public class MovementController : CachedMonobehaviour
    {
        protected Rigidbody _rigidbody;

        public Vector3 SimulationPosition => _rigidbody.position;
        public Quaternion SimulationRotation => _rigidbody.rotation;
        public float SimulationYaw => _rigidbody.rotation.eulerAngles.y;

        protected virtual void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        public Vector3 GetDirection(Vector3 targetPos)
        {
            Vector3 calDirection = targetPos - SimulationPosition;
            calDirection.y = 0f;

            return calDirection.normalized;
        }

        public float GetDistance(Vector3 targetPos)
        {
            return Vector3.Distance(SimulationPosition, targetPos);
        }

        public float GetDistance(Vector3 originPos, Vector3 targetPos)
        {
            float distance = Vector3.Distance(originPos, targetPos);
            return distance;
        }
    }
}