using UnityEngine;

public class IdleAction : UtilityAction
{
    private Vector3 targetPosition;
    private float idleTimer = 0f;
    private float idleInterval = 5f; // Pick new destination every 5 seconds

    public IdleAction(StrategistController strategist) : base(strategist) { }

    public override float CalculateUtility()
    {
        // fallback action
        // when nothing else is more important
        return 0.1f;
    }

    public override void Execute()
    {
        idleTimer += Time.deltaTime;

        // Pick a new random destination
        if (idleTimer >= idleInterval || !strategist.agent.hasPath)
        {
            idleTimer = 0f;
            PickRandomDestination();
        }
        // Set speed to normal
        strategist.agent.speed = strategist.normalSpeed;
    }

    void PickRandomDestination()
    {
        // Pick a random point within a radius
        Vector3 randomDirection = Random.insideUnitSphere * 20f;
        randomDirection += strategist.transform.position;
        randomDirection.y = strategist.transform.position.y; // Keep same height

        // Try to find a valid NavMesh position
        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(randomDirection, out hit, 20f, UnityEngine.AI.NavMesh.AllAreas))
        {
            targetPosition = hit.position;
            strategist.agent.SetDestination(targetPosition);
            Debug.Log("Strategist idling - moving to random position");
        }
    }
}