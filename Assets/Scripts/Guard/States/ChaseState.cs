using UnityEngine;

public class ChaseState : GuardState
{
    private float lostTargetTimer = 0f;
    private float maxLostTime = 2f;

    public ChaseState(GuardController guard) : base(guard) { }

    public override void Enter()
    {
        base.Enter();

        // Set chase speed (faster than patrol)
        guard.agent.speed = guard.chaseSpeed;

        lostTargetTimer = 0f;
    }

    public override void Update()
    {
        if (guard.CurrentTarget == null)
        {
            // Lost enemy, go to search
            guard.ChangeState(new SearchState(guard));
            return;
        }

        // Check for enemy
        if (guard.CanSeeTarget(guard.CurrentTarget))
        {
            // chase enemy seen
            guard.LastKnownPosition = guard.CurrentTarget.position;
            guard.agent.SetDestination(guard.CurrentTarget.position);
            lostTargetTimer = 0f;

            Debug.DrawLine(guard.transform.position, guard.CurrentTarget.position, Color.red);
            float distanceToTarget = Vector3.Distance(guard.transform.position, guard.CurrentTarget.position);
            if (distanceToTarget < guard.attackRange)
            {
                // Stop moving and attack
                guard.agent.SetDestination(guard.transform.position);

                // Attack with cooldown
                if (Time.time - guard.lastAttackTime > guard.attackCooldown)
                {
                    guard.lastAttackTime = Time.time;

                    // Try attack Strategist
                    StrategistController strategist = guard.CurrentTarget.GetComponent<StrategistController>();
                    if (strategist != null)
                    {
                        strategist.TakeDamage(guard.attackDamage);
                        Debug.Log($"Guard attacked Strategist for {guard.attackDamage} damage!");
                    }
                }

            }
        }
        else
        {
            // Can't see enemy anymore
            lostTargetTimer += Time.deltaTime;

            if (lostTargetTimer >= maxLostTime)
            {
                // Lost enemy for too long, switch to Search
                guard.ChangeState(new SearchState(guard));
            }
            else
            {
                // Keep chasing last known position briefly
                guard.agent.SetDestination(guard.LastKnownPosition);
            }
        }


    }



    public override void Exit()
    {
        base.Exit();
    }
}