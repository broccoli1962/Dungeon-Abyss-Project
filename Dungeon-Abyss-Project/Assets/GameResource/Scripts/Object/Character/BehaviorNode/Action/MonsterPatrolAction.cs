using System;
using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Backend.Object.Character.Monster;
using Backend.Object.Character.Monster.Normal;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "MonsterPatrolAction",
    story: "[Self] Patrols [PatrolPoints] using [MovementController]",
    category: "Action",
    id: "4b1649bad2c0bc979970daeee927d151")]
public partial class MonsterPatrolAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<NormalMovementController> MovementController;
    [SerializeReference] public BlackboardVariable<List<GameObject>> PatrolPoints;
    [SerializeReference] public BlackboardVariable<MonsterStatus> MonsterStatus;
    [SerializeReference] public BlackboardVariable<string> AnimatorSpeedParam = new("Speed");
    
    [CreateProperty] private int _currentIndex = 0;
    [CreateProperty] private float _waitTimer;
    [CreateProperty] private bool _isWaiting;

    private Animator _animator;

    protected override Status OnStart()
    {
        if (Self.Value == null || MonsterStatus.Value == null || PatrolPoints.Value == null || PatrolPoints.Value.Count == 0)
        {
            return Status.Failure;
        }

        _animator = Self.Value.GetComponentInChildren<Animator>();

        _isWaiting = false;
        _waitTimer = 0f;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if(MovementController.Value == null) return Status.Failure;

        if (_isWaiting)
        {
            if(_waitTimer > 0f)
            {
                _waitTimer -= Time.deltaTime;
                MovementController.Value.Stop();
                UpdateAnimator(0f);
                return Status.Running;
            }
            else
            {
                _isWaiting = false;
                MoveToNextIndex();
            }
        }

        Transform targetWayPoint = PatrolPoints.Value[_currentIndex].transform;

        Vector3 flatTargetPos = new Vector3(targetWayPoint.position.x, Self.Value.transform.position.y, targetWayPoint.position.z);
        float distance = MovementController.Value.GetDistance(flatTargetPos);

        if(distance <= 0.5f)
        {
            _waitTimer = 2.0f;

            _isWaiting = true;
            return Status.Running;
        }

        MovementController.Value.MoveToDestination(targetWayPoint.position, MonsterStatus.Value.WalkSpeed, MonsterStatus.Value.RotSpeed);

        //�ȴ� blendTree speed animation
        UpdateAnimator(0.5f);

        return Status.Running;
    }

    protected override void OnEnd()
    {
       if(MovementController.Value != null)
       {
            MovementController.Value.Stop();
       }
       UpdateAnimator(0f);
    }

    private void MoveToNextIndex()
    {
        _currentIndex = (_currentIndex + 1) % PatrolPoints.Value.Count;
    }

    private void UpdateAnimator(float speed)
    {
        if(_animator != null)
        {
            _animator.SetFloat(AnimatorSpeedParam, speed, 0.1f, Time.deltaTime);
        }
    }
}