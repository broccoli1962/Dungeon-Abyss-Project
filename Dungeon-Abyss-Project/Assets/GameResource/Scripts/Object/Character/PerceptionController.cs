using DdaIT.Util;
using UnityEngine;

namespace DdaIT.Object.Character
{
    /// <summary>
    /// 전방 원뿔(수평 FOV) 기준 시야. 몸 pose는 <see cref="SetSimulationPose"/>로 SessionActor(또는 sim)가 주입.
    /// </summary>
    public abstract class PerceptionController : CachedMonobehaviour
    {
        [Header("Sight (Base)")]
        [SerializeField] private float _viewAngleDegrees = 90f;
        [SerializeField] private float _viewDistance = 12f;
        [SerializeField] private float _eyeHeight = 1.2f;

        [Header("Gizmo")]
        [SerializeField] private bool _drawGizmos = true;
        [ShowIf(ActionOnConditionFail.DontDraw, ConditionOperator.And, nameof(_drawGizmos))]
        [SerializeField] private Color _gizmoFillColor = new(1f, 0.92f, 0.2f, 0.25f);
        [ShowIf(ActionOnConditionFail.DontDraw, ConditionOperator.And, nameof(_drawGizmos))]
        [SerializeField] private Color _gizmoLineColor = new(1f, 0.85f, 0.1f, 0.9f);
        [ShowIf(ActionOnConditionFail.DontDraw, ConditionOperator.And, nameof(_drawGizmos))]
        [SerializeField] [Min(4)] private int _gizmoArcSegments = 24;

        private Vector3 _simulationBodyPosition;
        private Quaternion _simulationBodyRotation = Quaternion.identity;
        private bool _hasSimulationPose;

        public float ViewAngleDegrees => _viewAngleDegrees;
        public float ViewDistance => _viewDistance;
        public bool HasSimulationPose => _hasSimulationPose;

        public virtual float EffectiveViewDistance => _viewDistance;
        public virtual float EffectiveViewAngleDegrees => _viewAngleDegrees;

        /// <summary>RunState / Movement sim 등 Actor가 매 틱 주입하는 권위 pose.</summary>
        public void SetSimulationPose(Vector3 bodyWorldPosition, Quaternion bodyWorldRotation)
        {
            _simulationBodyPosition = bodyWorldPosition;
            _simulationBodyRotation = bodyWorldRotation;
            _hasSimulationPose = true;
        }

        public void SetSimulationPose(Vector3 bodyWorldPosition, float yawDegrees)
        {
            SetSimulationPose(bodyWorldPosition, Quaternion.Euler(0f, yawDegrees, 0f));
        }

        public bool CanSee(Transform target)
        {
            if (target == null)
                return false;

            return CanSeePoint(target.position);
        }

        public bool CanSeePoint(Vector3 worldPosition)
        {
            Vector3 origin = GetEyeWorldPosition();
            Vector3 toTarget = worldPosition - origin;
            toTarget.y = 0f;

            float distance = toTarget.magnitude;
            if (distance <= Mathf.Epsilon)
                return true;

            if (distance > EffectiveViewDistance)
                return false;

            float effectiveAngle = EffectiveViewAngleDegrees;
            if (effectiveAngle >= 360f - Mathf.Epsilon)
                return true;

            float halfAngle = effectiveAngle * 0.5f;
            Vector3 forward = GetFlatForward();
            float angle = Vector3.Angle(forward, toTarget.normalized);
            return angle <= halfAngle;
        }

        protected Vector3 GetBodyWorldPosition()
        {
            return _hasSimulationPose ? _simulationBodyPosition : CachedTransform.position;
        }

        protected Vector3 GetEyeWorldPosition()
        {
            return GetBodyWorldPosition() + Vector3.up * _eyeHeight;
        }

        protected Vector3 GetFlatForward()
        {
            Vector3 forward = _hasSimulationPose
                ? _simulationBodyRotation * Vector3.forward
                : CachedTransform.forward;

            forward.y = 0f;
            return forward.sqrMagnitude > Mathf.Epsilon ? forward.normalized : Vector3.forward;
        }

        protected virtual void OnDrawGizmosSelected()
        {
            if (!_drawGizmos)
                return;

            DrawSightGizmo();
        }

        private void DrawSightGizmo()
        {
            Vector3 origin = GetEyeWorldPosition();
            float distance = EffectiveViewDistance;
            float effectiveAngle = EffectiveViewAngleDegrees;
            Vector3 forward = GetFlatForward();

            Gizmos.color = _gizmoLineColor;

            if (effectiveAngle >= 360f - Mathf.Epsilon)
            {
                DrawCircleGizmo(origin, distance);
                return;
            }

            float halfAngle = effectiveAngle * 0.5f;
            Vector3 leftDir = Quaternion.Euler(0f, -halfAngle, 0f) * forward;
            Vector3 rightDir = Quaternion.Euler(0f, halfAngle, 0f) * forward;

            Gizmos.DrawLine(origin, origin + leftDir * distance);
            Gizmos.DrawLine(origin, origin + rightDir * distance);

            Vector3 previousPoint = origin + leftDir * distance;
            for (int i = 1; i <= _gizmoArcSegments; i++)
            {
                float t = i / (float)_gizmoArcSegments;
                float yaw = Mathf.Lerp(-halfAngle, halfAngle, t);
                Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * forward;
                Vector3 point = origin + dir * distance;
                Gizmos.DrawLine(previousPoint, point);
                previousPoint = point;
            }

            Gizmos.color = _gizmoFillColor;
            for (int i = 0; i < _gizmoArcSegments; i++)
            {
                float t0 = i / (float)_gizmoArcSegments;
                float t1 = (i + 1) / (float)_gizmoArcSegments;
                float yaw0 = Mathf.Lerp(-halfAngle, halfAngle, t0);
                float yaw1 = Mathf.Lerp(-halfAngle, halfAngle, t1);
                Vector3 p0 = origin + Quaternion.Euler(0f, yaw0, 0f) * forward * distance;
                Vector3 p1 = origin + Quaternion.Euler(0f, yaw1, 0f) * forward * distance;
                Gizmos.DrawLine(origin, p0);
                Gizmos.DrawLine(origin, p1);
                Gizmos.DrawLine(p0, p1);
            }
        }

        private static void DrawCircleGizmo(Vector3 origin, float radius)
        {
            const int segments = 32;
            Vector3 previous = origin + Vector3.forward * radius;
            for (int i = 1; i <= segments; i++)
            {
                float rad = i / (float)segments * Mathf.PI * 2f;
                Vector3 point = origin + new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * radius;
                Gizmos.DrawLine(previous, point);
                previous = point;
            }
        }
    }
}
