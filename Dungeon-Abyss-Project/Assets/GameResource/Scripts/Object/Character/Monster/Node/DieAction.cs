using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace DdaIT.Object.Character.Monster
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Die",
        story: "[Self] dies",
        category: "Action/Monster",
        id: "e8b1d4a06c3f4e7a9b2d5f8c1a4e7036")]
    public partial class DieAction : Action
    {
        [SerializeReference] public BlackboardVariable<MonsterSessionActor> Self;

        protected override Status OnStart()
        {
            MonsterSessionActor actor = Self?.Value;
            if (actor == null)
                return Status.Failure;

            actor.Movement.StopMove();
            actor.Animation.SetMoveSpeed(0f);
            actor.Animation.PlayDeath();
            return Status.Running;
        }

        // Success로 끝나면 On Start Repeat가 대기 가지로 다시 내려간다.
        protected override Status OnUpdate() => Status.Running;
    }
}
