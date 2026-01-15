using UnityEngine;

public class SearchState : GuardState
{
    private float searchTimer = 0f;
    private bool hasReachedSearchPosition = false;

    public SearchState(GuardController guard) : base(guard) { }

    public override void Enter()
    {
        base.Enter();

        // Set normal speed
        guard.agent.speed = guard.normalSpeed;

        // Move to last known position
        guard.agent.SetDestination(guard.LastKnownPosition);
        searchTimer = 0f;
        hasReachedSearchPosition = false;

        Debug.Log("Searching at last known position: " + guard.LastKnownPosition);
    }

    public override void Update()
    {
        // Check if enemy is spotted again
        Transform detectedTarget = guard.DetectTarget();
        if (detectedTarget != null)
        {
            // Found target again! Back to chase
            guard.CurrentTarget = detectedTarget;
            guard.ChangeState(new ChaseState(guard));
            return;
        }

        // Check if reached search position
        if (!hasReachedSearchPosition)
        {
            if (!guard.agent.pathPending && guard.agent.remainingDistance <= guard.agent.stoppingDistance)
            {
                if (!guard.agent.hasPath || guard.agent.velocity.sqrMagnitude == 0f)
                {
                    hasReachedSearchPosition = true;
                    Debug.Log("Reached search position, searching area...");
                }
            }
        }

        // Count search time
        searchTimer += Time.deltaTime;

        // After search duration expires, return to patrol
        if (searchTimer >= guard.searchDuration)
        {
            Debug.Log("Search complete, returning to patrol");
            guard.CurrentTarget = null;
            guard.ChangeState(new PatrolState(guard));
        }
    }

    public override void Exit()
    {
        base.Exit();
    }
}