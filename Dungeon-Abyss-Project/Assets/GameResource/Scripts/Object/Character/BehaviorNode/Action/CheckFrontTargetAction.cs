using Backend.Object.Character.Monster;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "CheckFrontTarget", story: "Check if [NttDetector] in [Self] Front", category: "Action", id: "449c2007581718f90f3b98899bcdde78")]
public partial class CheckFrontTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<MonsterDetectController> NttDetector;
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    protected override Status OnStart()
    {
        Vector3 toTarget = NttDetector.Value.CurrentTarget.transform.position - Self.Value.transform.position;
        toTarget.y = 0;
        Vector3 front = Self.Value.transform.forward;
        front.y = 0;

        toTarget.Normalize();
        front.Normalize();

        float dot = Vector3.Dot(front, toTarget);
        float threshold = Mathf.Cos(10f * Mathf.Deg2Rad);

        return dot >= threshold ? Status.Success : Status.Failure;
    }
}