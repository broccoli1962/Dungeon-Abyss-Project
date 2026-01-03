using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Backend.Object.Character.Player
{
    public class FootIKHandler : MonoBehaviour
    {
        public Transform leftFootTarget;  // 아까 만든 Target 오브젝트
        public Transform rightFootTarget;
        public LayerMask groundLayer;     // 땅으로 인식할 레이어
        public float footOffset = 0.1f;   // 발바닥 두께 조절용

        void Update()
        {
            AdjustFootTarget(leftFootTarget);
            AdjustFootTarget(rightFootTarget);
        }

        void AdjustFootTarget(Transform target)
        {
            // 발 위치보다 조금 위에서 아래로 레이를 쏩니다.
            Ray ray = new Ray(target.position + Vector3.up, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, 2f, groundLayer))
            {
                // 레이가 바닥에 맞으면 해당 위치로 Target을 이동시킵니다.
                Vector3 footPos = hit.point;
                footPos.y += footOffset;
                target.position = footPos;

                // 바닥의 기울기에 맞춰 발의 회전값도 조절 (선택사항)
                target.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * transform.rotation;
            }
        }
    }
}