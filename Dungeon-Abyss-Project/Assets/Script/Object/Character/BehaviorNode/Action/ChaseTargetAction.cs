using Backend.Object.Character.Monster;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Backend.Object.Character.Monster.Normal;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ChaseTarget", story: "Move [Self] to the [NttDetector] target [NttStatus],[MovementController]", category: "Action", id: "978a5b89230b820e0d38fcba7a721a72")]
public partial class ChaseTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<MonsterDetectController> NttDetector;
    [SerializeReference] public BlackboardVariable<MonsterStatus> NttStatus;
    [SerializeReference] public BlackboardVariable<NormalMovementController> MovementController;

    protected override Status OnStart()
    {
        if(MovementController.Value == null || NttDetector.Value.CurrentTarget == null)
        {
            return Status.Failure;
        }
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        Transform target = NttDetector.Value.CurrentTarget.transform;
        if (target == null)
        {
            return Status.Failure;   
        }

        float distance = Vector3.Distance(Self.Value.transform.position, target.position);
        if(distance <= 2.0f)
        {
            return Status.Success;
        }

        MovementController.Value.MoveToDestination(target.position, NttStatus.Value.SprintSpeed, NttStatus.Value.RotSpeed);
        return Status.Running;
    }

    protected override void OnEnd()
    {
        if(MovementController.Value != null)
        {
            MovementController.Value.Stop();
        }
    }
}

