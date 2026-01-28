using UnityEngine;
using UnityEngine.AI;

public class GuardController : MonoBehaviour
{
    [Header("Components")]
    public NavMeshAgent agent;

    [Header("Patrol Settings")]
    public Transform[] patrolPoints;
    public float patrolWaitTime = 2f;

    [Header("Detection Settings")]
    public float detectionRadius = 10f;
    public float fieldOfViewAngle = 110f;
    public LayerMask targetMask; // What can be detected (enemies)
    public LayerMask obstacleMask; // What blocks vision (walls)

    [Header("Chase Settings")]
    public float chaseSpeed = 5f;
    public float normalSpeed = 3.5f;

    [Header("Search Settings")]
    public float searchDuration = 5f;
    public float searchRadius = 5f;

    [Header("Combat")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public float attackDamage = 15f;
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;
    public float lastAttackTime = 1f;


    // State Machine
    private GuardState currentState;

    // Public properties for states to access
    public Transform CurrentTarget { get; set; }
    public Vector3 LastKnownPosition { get; set; }

    void Start()
    {
        // Get NavMeshAgent component
        agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component missing!");
            return;
        }

        // Validate patrol points
        if (patrolPoints.Length == 0)
        {
            Debug.LogWarning("No patrol points assigned! Patrol state won't work properly.");
        }

        // Start in Patrol state
        ChangeState(new PatrolState(this));
    }

    void Update()
    {
        // Update current state
        currentState?.Update();
    }

    /// <summary>
    /// Change to a new state
    /// </summary>
    public void ChangeState(GuardState newState)
    {
        // Exit current state
        currentState?.Exit();

        // Change to new state
        currentState = newState;

        // Enter new state
        currentState?.Enter();
    }

    /// <summary>
    /// Check if any target is visible within detection range
    /// </summary>
    public Transform DetectTarget()
    {
        // Find all colliders within detection radius
        Collider[] targetsInRadius = Physics.OverlapSphere(transform.position, detectionRadius, targetMask);

        foreach (Collider targetCollider in targetsInRadius)
        {
            Transform target = targetCollider.transform;
            Vector3 directionToTarget = (target.position - transform.position).normalized;

            // Check if target is within field of view angle
            if (Vector3.Angle(transform.forward, directionToTarget) < fieldOfViewAngle / 2)
            {
                float distanceToTarget = Vector3.Distance(transform.position, target.position);

                // Check if we have clear line of sight (no obstacles blocking)
                if (!Physics.Raycast(transform.position, directionToTarget, distanceToTarget, obstacleMask))
                {
                    // Target detected!
                    return target;
                }
            }
        }

        return null; // No target detected
    }

    /// <summary>
    /// Check if we can still see the current target
    /// </summary>
    public bool CanSeeTarget(Transform target)
    {
        if (target == null) return false;

        Vector3 directionToTarget = (target.position - transform.position).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        // Check distance
        if (distanceToTarget > detectionRadius)
            return false;

        // Check angle
        if (Vector3.Angle(transform.forward, directionToTarget) > fieldOfViewAngle / 2)
            return false;

        // Check line of sight
        if (Physics.Raycast(transform.position, directionToTarget, distanceToTarget, obstacleMask))
            return false;

        return true;
    }

    public void TakeDamage(float damage, Transform attacker = null)
    {
        currentHealth -= damage;
        Debug.Log($"Guard took {damage} damage! Health: {currentHealth}/{maxHealth}");

        // React to damage!
        if (attacker != null)
        {
            // Remember who attacked us
            CurrentTarget = attacker;
            LastKnownPosition = attacker.position;

            // Immediately switch to Chase state
            ChangeState(new ChaseState(this));

            Debug.Log($"<color=orange>Guard reacting to damage! Switching to Chase!</color>");
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Guard died!");
        gameObject.SetActive(false);
    }

    // Visual debugging in Scene view
    void OnDrawGizmosSelected()
    {
        // Draw detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Draw field of view
        Vector3 leftBoundary = Quaternion.Euler(0, -fieldOfViewAngle / 2, 0) * transform.forward * detectionRadius;
        Vector3 rightBoundary = Quaternion.Euler(0, fieldOfViewAngle / 2, 0) * transform.forward * detectionRadius;

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);

        // Draw path if available
        if (agent != null && agent.hasPath)
        {
            Gizmos.color = Color.green;
            Vector3[] corners = agent.path.corners;

            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
        }
    }
}