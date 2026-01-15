using UnityEngine;
using UnityEngine.AI;

public class GuardController : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float attackRange = 2f;

    private int currentWaypointIndex = 0;
    private bool isPatrolling = true;
    private bool isChasing = false;

    void Start()
    {
        agent.speed = patrolSpeed;
        agent.SetDestination(waypoints[currentWaypointIndex].position);
    }

    void Update()
    {
        if (isPatrolling)
        {
            Patrol();
        }
        else if (isChasing)
        {
            Chase();
        }
    }

    void Patrol()
    {
        if (Vector3.Distance(transform.position, waypoints[currentWaypointIndex].position) < 1f)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Length)
            {
                currentWaypointIndex = 0;
            }
            agent.SetDestination(waypoints[currentWaypointIndex].position);
        }

        if (Vector3.Distance(transform.position, waypoints[currentWaypointIndex].position) < detectionRange)
        {
            isPatrolling = false;
            isChasing = true;
            agent.speed = chaseSpeed;
        }
    }

    void Chase()
    {
        // Implement chasing logic here
        // For now, just move towards the player
        // You can add more sophisticated chasing behavior later
    }

    void Search()
    {
        // Implement searching logic here
        // For now, just return to patrolling
        isChasing = false;
        isPatrolling = true;
        agent.speed = patrolSpeed;
        agent.SetDestination(waypoints[currentWaypointIndex].position);
    }
}
