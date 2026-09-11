using UnityEngine;

namespace DdaIT.Object.Character.Monster
{
    public class MonsterStatus : MonoBehaviour
    {
        [field: SerializeField] public float RotSpeed { get; set; }
        [field: SerializeField] public float WalkSpeed { get; set; }
        [field: SerializeField] public float SprintSpeed { get; set; }
        [field: SerializeField] public float AttackRange { get; private set; }
    }
}
