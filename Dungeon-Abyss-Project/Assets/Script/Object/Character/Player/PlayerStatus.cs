using UnityEngine;

namespace Backend.Object.Character.Player
{
    public class PlayerStatus : Status
    {
        [field: SerializeField] public float RotSpeed { get; set; }
        [field: SerializeField] public float WalkSpeed { get; set; }
        [field: SerializeField] public float SprintSpeed { get; set; }

    }
}