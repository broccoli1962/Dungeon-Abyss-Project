using UnityEngine;

namespace Backend.Object.Character.Monster
{
    public class MonsterStatus : Status
    {
        [field: SerializeField] public float AttackRange { get; private set; }
    }
}
