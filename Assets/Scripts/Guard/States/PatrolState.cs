using UnityEngine;

public class PatrolState : GuardState
{
    private int currentPatrolIndex = 0;
    private float waitTimer = 0f;
    private bool isWaiting = false;

    public PatrolState(GuardController guard) : base(guard) { }

    public override void Enter()
    {
        base.Enter();

        // Set normal patrol speed
        guard.agent.speed = guard.normalSpeed;

        // Go to first patrol point
        if (guard.patrolPoints.Length > 0)
        {
            MoveToNextPatrolPoint();
        }
    }

    public override void Update()
    {
        // Check for enemies
        Transform detectedTarget = guard.DetectTarget();
        if (detectedTarget != null)
        {
            // Enemy spotted, Switch to Chase
            guard.CurrentTarget = detectedTarget;
            guard.ChangeState(new ChaseState(guard));
            return;
        }

        // Handle patrol logic
        if (guard.patrolPoints.Length == 0) return;

        if (isWaiting)
        {
            // Wait at patrol point
            waitTimer += Time.deltaTime;

            if (waitTimer >= guard.patrolWaitTime)
            {
                // Finished waiting, move to next point
                isWaiting = false;
                MoveToNextPatrolPoint();
            }
        }
        else
        {
            // Check if reached patrol point
            if (!guard.agent.pathPending && guard.agent.remainingDistance <= guard.agent.stoppingDistance)
            {
                if (!guard.agent.hasPath || guard.agent.velocity.sqrMagnitude == 0f)
                {
                    // Reached patrol point, start waiting
                    isWaiting = true;
                    waitTimer = 0f;
                }
            }
        }
    }

    private void MoveToNextPatrolPoint()
    {
        if (guard.patrolPoints.Length == 0) return;

        // Move to current patrol point
        guard.agent.SetDestination(guard.patrolPoints[currentPatrolIndex].position);

        // Cycle to next point
        currentPatrolIndex = (currentPatrolIndex + 1) % guard.patrolPoints.Length;

        Debug.Log($"Moving to patrol point {currentPatrolIndex}");
    }

    public override void Exit()
    {
        base.Exit();
        isWaiting = false;
    }
}