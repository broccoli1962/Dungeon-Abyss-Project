using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace DdaIT.Object.Character.Monster
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Chase Target",
        story: "[Self] runs to [Target]",
        category: "Action/Monster",
        id: "6a91d0e2c4b74f8e9a1c3d5b7e0f2468")]
    public partial class ChaseTargetAction : Action
    {
        [SerializeReference] public BlackboardVariable<MonsterSessionActor> Self;
        [SerializeReference] public BlackboardVariable<GameObject> Target;
        [SerializeReference] public BlackboardVariable<float> StopDistance = new(1.6f);

        protected override Status OnStart()
        {
            if (Self?.Value == null || Target?.Value == null)
                return Status.Failure;

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            MonsterSessionActor actor = Self?.Value;
            GameObject target = Target?.Value;
            if (actor == null || target == null)
                return Status.Failure;

            Vector3 pos = target.transform.position;
            if (actor.Movement.GetDistance(pos) <= StopDistance.Value)
                return Status.Success;

            actor.Movement.RunTo(pos);
            actor.Movement.RotateTo(pos);
            actor.Animation.SetMoveSpeed(1f);
            return Status.Running;
        }

        protected override void OnEnd()
        {
            MonsterSessionActor actor = Self?.Value;
            if (actor == null)
                return;

            actor.Movement.StopMove();
            actor.Animation.SetMoveSpeed(0f);
        }
    }
}
