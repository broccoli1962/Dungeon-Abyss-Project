using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace DdaIT.Object.Character.Monster
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Idle",
        story: "[Self] idles",
        category: "Action/Monster",
        id: "1c9e7a4b2d8f4c6e0a5b3d7f9e2c1840")]
    public partial class IdleAction : Action
    {
        [SerializeReference] public BlackboardVariable<MonsterSessionActor> Self;

        protected override Status OnStart()
        {
            MonsterSessionActor actor = Self?.Value;
            if (actor == null)
                return Status.Failure;

            actor.Movement.StopMove();
            actor.Animation.SetMoveSpeed(0f);
            return Status.Success;
        }
    }
}
