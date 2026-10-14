using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

[assembly: GeneratePropertyBagsForAssembly]

namespace DdaIT.Object.Character.Monster
{
    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "Within Range",
        story: "[Self] is within [Range] of [Target]",
        category: "Monster",
        id: "b7e1c4a09d6f4e2a8c5b1d3f7a9e0c42")]
    public partial class WithinRangeCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<MonsterSessionActor> Self;
        [SerializeReference] public BlackboardVariable<GameObject> Target;
        [SerializeReference] public BlackboardVariable<float> Range;

        public override bool IsTrue()
        {
            if (Self?.Value == null || Target?.Value == null || Range == null)
                return false;

            float distance = Self.Value.Movement.GetDistance(Target.Value.transform.position);
            return distance <= Range.Value;
        }
    }
}
