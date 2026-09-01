using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SetAnimationDampFloat", story: "Set [AnimParm] in [Animator] to [float]", category: "Action", id: "868d2589b838e4b32ffcb90d643688ec")]
public partial class SetAnimationDampFloatAction : Action
{
    [SerializeReference] public BlackboardVariable<string> AnimParm;
    [SerializeReference] public BlackboardVariable<Animator> Animator;
    [SerializeReference] public BlackboardVariable<float> Float;

    protected override Status OnStart()
    {
        if (Animator.Value == null)
        {
            LogFailure("No Animator set.");
            return Status.Failure;
        }

        Animator.Value.SetFloat(AnimParm.Value, Float.Value, 0.1f, Time.deltaTime);
        return Status.Success;
    }
}

