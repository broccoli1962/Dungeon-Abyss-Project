using UnityEngine;

namespace Backend.Object.Character
{
    public class Status : MonoBehaviour, IValueChangable
    {
        [field: SerializeField] public string CharacterName { get; set; }
        [field: SerializeField] public float CurrentHealth { get; set; }
        [field: SerializeField] public float MaxHealth { get; set; }
        [field: SerializeField] public float RotSpeed { get; set; }
        [field: SerializeField] public float WalkSpeed { get; set; }
        [field: SerializeField] public float SprintSpeed { get; set; }
        [field: SerializeField] public float PhysicsDamage { get; set; }
        [field: SerializeField] public float MagicDamage { get; set; }

        [SerializeField, Range(0, 1)] public float _sightRange; 
        public float SightRange
        {
            get 
            {
                return _sightRange * 360f;
            }
            private set { }
        }

        public void ValueChange(float value)
        {
            
        }
    }
}