using UnityEngine;

namespace DdaIT.Object.Character
{
    public class WeaponHitVolume : MonoBehaviour
    {
        [SerializeField] private Transform _origin;
        [SerializeField] private Vector3 _halfExtends = new(0.3f, 0.3f, 0.6f);
        [SerializeField] private LayerMask _targetMask;

        public Vector3 Position => _origin.position;
        public Quaternion Rotation => _origin.rotation;
        public Vector3 HalfExtends => _halfExtends;
        public LayerMask TargetMask => _targetMask;

        #if UNITY_EDITOR
        private void OnDrawGizmosSelected(){
            if(_origin == null) return;
            Gizmos.matrix = Matrix4x4.TRS(_origin.position, _origin.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, HalfExtends * 2f);
        }
        #endif
    }
}
