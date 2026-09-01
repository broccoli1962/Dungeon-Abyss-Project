using Backend.Object.Character.Monster;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Backend.Object.Character.Monster.Normal;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "CheckDistanceTarget", story: "Check if [target] in [NttStatus] [Self] Range", category: "Action", id: "54ef61f7958619655e9fd9f0a571de44")]
public partial class CheckDistanceTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<MonsterDetectController> Target;
    [SerializeReference] public BlackboardVariable<MonsterStatus> NttStatus;
    [SerializeReference] public BlackboardVariable<NormalMovementController> NttMovement;

    protected override Status OnStart()
    {
        if (Target.Value.CurrentTarget == null) return Status.Failure;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if(Target.Value.CurrentTarget == null) return Status.Failure;

        var targetPos = Target.Value.CurrentTarget.transform.position;
        var selfPos = Self.Value.transform.position;

        float distance = Vector3.Distance(targetPos, selfPos);

        if (distance <= NttStatus.Value.AttackRange)
        {
            return Status.Success;
        }
        return Status.Failure;
    }
}
