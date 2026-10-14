using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace DdaIT.Object.Character.Monster
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Hold",
        story: "[Self] holds while [IsStunned]",
        category: "Action/Monster",
        id: "a3f5c8e17b2d4a96b0e4d7c1f8a25903")]
    public partial class HoldAction : Action
    {
        [SerializeReference] public BlackboardVariable<MonsterSessionActor> Self;
        [SerializeReference] public BlackboardVariable<bool> IsStunned;

        protected override Status OnStart()
        {
            if (Self?.Value == null || IsStunned == null)
                return Status.Failure;

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            MonsterSessionActor actor = Self?.Value;
            if (actor == null || IsStunned == null)
                return Status.Failure;

            actor.Movement.StopMove();
            actor.Animation.SetMoveSpeed(0f);
            return IsStunned.Value ? Status.Running : Status.Success;
        }
    }
}
