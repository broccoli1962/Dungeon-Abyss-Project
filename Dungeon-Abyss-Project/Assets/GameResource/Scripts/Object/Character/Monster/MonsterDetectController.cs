using UnityEngine;

namespace Backend.Object.Character.Monster
{
    public class MonsterDetectController : MonoBehaviour
    {
        [Header("Detect Settings")]
        [Range(0, 360), SerializeField] private float _viewAngle = 80f;
        [SerializeField] private float _detectRadius = 10f;
        [SerializeField] private LayerMask _targetLayer;
        [SerializeField] private LayerMask _obstacleLayer;
        [SerializeField] private int _maxTarget;

        private Collider[] _overlapCollider;
        private float _sqrDetectRadius;
        private GameObject _target;
        public GameObject CurrentTarget { get { return _target; } private set { } }

        private void Awake()
        {
            _overlapCollider = new Collider[_maxTarget];
            _sqrDetectRadius = _detectRadius * _detectRadius;

            _targetLayer = LayerMask.GetMask("Player");
        }

        private void FixedUpdate()
        {
            OnDetect();
        }

        private void OnDetect()
        {
            int found = Physics.OverlapSphereNonAlloc(transform.position, _detectRadius, _overlapCollider, _targetLayer);

            GameObject target = null;
            float closestSqrDist = _sqrDetectRadius;

            for (int i = 0; i < found; i++)
            {
                Transform targetTransform = _overlapCollider[i].transform;
                Vector3 dirToTarget = (targetTransform.position - transform.position).normalized;
                float sqrDistToTarget = dirToTarget.sqrMagnitude;

                if (Vector3.Angle(transform.forward, dirToTarget.normalized) < _viewAngle * 0.5f)
                {
                    if (!Physics.Raycast(transform.position, dirToTarget.normalized, Mathf.Sqrt(sqrDistToTarget), _obstacleLayer))
                    {
                        if (sqrDistToTarget < closestSqrDist)
                        {
                            closestSqrDist = sqrDistToTarget;
                            target = _overlapCollider[i].gameObject;
                        }
                    }
                }
            }

            SetTarget(target);

            for(int i = 0; i < found; i++)
            {
                _overlapCollider[i] = null;
            }
        }

        private void SetTarget(GameObject target)
        {
            _target = target;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // �þ� ���� �� �׸���
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, _detectRadius);

            // �þ߰� ���� �׸���
            Vector3 lookAngle01 = DirFromAngle(-_viewAngle / 2, false);
            Vector3 lookAngle02 = DirFromAngle(_viewAngle / 2, false);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + lookAngle01 * _detectRadius);
            Gizmos.DrawLine(transform.position, transform.position + lookAngle02 * _detectRadius);

            if (CurrentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, CurrentTarget.transform.position);
            }
        }

        private Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
        {
            if (!angleIsGlobal)
            {
                angleInDegrees += transform.eulerAngles.y;
            }
            return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
        }
    }
#endif
}