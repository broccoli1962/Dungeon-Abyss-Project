using UnityEngine;

namespace Backend.Object.Character
{
    [RequireComponent(typeof(Rigidbody))]
    public class MovementController : MonoBehaviour
    {
        protected Rigidbody _rigidbody;

        protected virtual void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        public Vector3 GetDirection(Vector3 targetPos)
        {
            Vector3 calDirection = targetPos - transform.position;
            calDirection.y = 0f;

            return calDirection.normalized;
        }

        public float GetDistance(Vector3 targetPos)
        {
            float distance = Vector3.Distance(transform.position, targetPos);
            return distance;
        }

        public float GetDistance(Vector3 originPos, Vector3 targetPos)
        {
            float distance = Vector3.Distance(originPos, targetPos);
            return distance;
        }
    }
}