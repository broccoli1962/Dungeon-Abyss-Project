using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace DdaIT.Object.Character.Monster
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Attack Target",
        story: "[Self] attacks [Target]",
        category: "Action/Monster",
        id: "d4c8b2a176e94f0b8e3a5c7d9f102346")]
    public partial class AttackTargetAction : Action
    {
        [SerializeReference] public BlackboardVariable<MonsterSessionActor> Self;
        [SerializeReference] public BlackboardVariable<GameObject> Target;

        protected override Status OnStart()
        {
            MonsterSessionActor actor = Self?.Value;
            if (actor == null || Target?.Value == null)
                return Status.Failure;

            actor.Movement.StopMove();
            actor.Movement.RotateTo(Target.Value.transform.position);
            return actor.Combat.TryPlayAttack() ? Status.Running : Status.Failure;
        }

        protected override Status OnUpdate()
        {
            MonsterSessionActor actor = Self?.Value;
            if (actor == null)
                return Status.Failure;

            if (Target?.Value != null)
                actor.Movement.RotateTo(Target.Value.transform.position);

            return actor.Combat.IsAttacking ? Status.Running : Status.Success;
        }

        protected override void OnEnd()
        {
            MonsterCombatController combat = Self?.Value?.Combat;
            if (combat != null && combat.IsAttacking)
                combat.OnAttackEnd();
        }
    }
}
