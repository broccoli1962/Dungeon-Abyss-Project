using Backend.Object.Character.Monster;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "CheckDetector", story: "Check if [TargetDetector] has a target and set [TargetDetected] flag", category: "Action", id: "5f120190b8bfdd6229c265faa5143672")]
public partial class CheckDetectorAction : Action
{
    [SerializeReference] public BlackboardVariable<MonsterDetectController> TargetDetector;
    [SerializeReference] public BlackboardVariable<bool> TargetDetected;

    protected override Status OnStart()
    {
        if(TargetDetector.Value == null)
        {
            TargetDetected.Value = false;
            return Status.Failure;
        }

        bool hasTarget = TargetDetector.Value.CurrentTarget != null;

        TargetDetected.Value = hasTarget;

        return hasTarget ? Status.Success : Status.Failure;
    }
}