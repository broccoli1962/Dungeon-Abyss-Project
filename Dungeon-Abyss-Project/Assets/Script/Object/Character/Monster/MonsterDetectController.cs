using System.Runtime.CompilerServices;
using Unity.Behavior;
using UnityEngine;

namespace Backend.Object.Character.Monster
{
    [RequireComponent(typeof(SphereCollider))]
    public class MonsterDetectController : MonoBehaviour
    {
        [Header("Detect Settings")]
        [SerializeField] private float _detectRadius = 10f;
        [SerializeField] private LayerMask _targetLayer;

        private SphereCollider _sensor;
        private GameObject _target;
        public GameObject CurrentTarget { get { return _target; } private set { } }

        private void Awake()
        {
            _sensor = GetComponent<SphereCollider>();
            _sensor.isTrigger = true;
            _sensor.radius = _detectRadius;

            _targetLayer = LayerMask.GetMask("Player");
        }

        private void OnTriggerEnter(Collider other)
        {
            if(IsInLayerMask(other.gameObject, _targetLayer))
            {
                SetTarget(other.gameObject);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if(IsInLayerMask(other.gameObject, _targetLayer))
            {
                SetTarget(null);
            }
        }

        private bool IsInLayerMask(GameObject obj, LayerMask layerMask)
        {
            return (layerMask.value & (1 << obj.layer)) != 0;
        }

        private void SetTarget(GameObject target)
        {
            _target = target;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _detectRadius);
        }
    }
#endif
}