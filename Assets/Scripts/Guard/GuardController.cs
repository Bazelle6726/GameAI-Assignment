using UnityEngine;
using UnityEngine.AI;

public class GuardController : MonoBehaviour
{
    #region Components
    [Header("Components")]
    public NavMeshAgent agent;
    #endregion

    #region Patrol Settings
    [Header("Patrol Settings")]
    public Transform[] patrolPoints;
    public float patrolWaitTime = 2f;
    #endregion

    #region Detection Settings
    [Header("Detection Settings")]
    public float detectionRadius = 10f;
    public float fieldOfViewAngle = 110f;
    public LayerMask targetMask;
    public LayerMask obstacleMask;
    #endregion

    #region Movement Settings
    [Header("Chase Settings")]
    public float chaseSpeed = 5f;
    public float normalSpeed = 3.5f;
    #endregion

    #region Search Settings
    [Header("Search Settings")]
    public float searchDuration = 5f;
    public float searchRadius = 5f;
    #endregion

    #region Combat Settings
    [Header("Combat")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public float attackDamage = 15f;
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;
    public float lastAttackTime = 0f;
    #endregion

    #region State Machine
    private GuardState currentState;

    public Transform CurrentTarget { get; set; }
    public Vector3 LastKnownPosition { get; set; }
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        InitializeComponents();
        ValidateSetup();

        ChangeState(new PatrolState(this));
    }

    void Update()
    {
        currentState?.Update();
    }
    #endregion

    #region Initialization
    private void InitializeComponents()
    {
        agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogError("[Guard] NavMeshAgent component missing!");
            enabled = false;
            return;
        }

        agent.speed = normalSpeed;
    }

    private void ValidateSetup()
    {
        if (patrolPoints.Length == 0)
        {
            Debug.LogWarning("[Guard] No patrol points assigned! Patrol state won't work properly.");
        }

        if (targetMask == 0)
        {
            Debug.LogWarning("[Guard] Target mask not set! Guard won't detect enemies.");
        }

        if (obstacleMask == 0)
        {
            Debug.LogWarning("[Guard] Obstacle mask not set! Line of sight checks may not work properly.");
        }
    }
    #endregion

    #region State Management
    public void ChangeState(GuardState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }
    #endregion

    #region Target Detection
    public Transform DetectTarget()
    {
        Collider[] targetsInRadius = Physics.OverlapSphere(transform.position, detectionRadius, targetMask);

        foreach (Collider targetCollider in targetsInRadius)
        {
            Transform target = targetCollider.transform;

            if (!target.gameObject.activeInHierarchy)
                continue;

            Vector3 directionToTarget = (target.position - transform.position).normalized;

            if (Vector3.Angle(transform.forward, directionToTarget) < fieldOfViewAngle / 2)
            {
                float distanceToTarget = Vector3.Distance(transform.position, target.position);

                if (!Physics.Raycast(transform.position, directionToTarget, distanceToTarget, obstacleMask))
                {
                    return target;
                }
            }
        }

        return null;
    }

    public bool CanSeeTarget(Transform target)
    {
        if (target == null)
            return false;

        if (!target.gameObject.activeInHierarchy)
            return false;

        Vector3 directionToTarget = (target.position - transform.position).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (distanceToTarget > detectionRadius)
            return false;

        if (Vector3.Angle(transform.forward, directionToTarget) > fieldOfViewAngle / 2)
            return false;

        if (Physics.Raycast(transform.position, directionToTarget, distanceToTarget, obstacleMask))
            return false;

        return true;
    }

    public bool IsTargetValid()
    {
        if (CurrentTarget == null)
            return false;

        if (!CurrentTarget.gameObject.activeInHierarchy)
            return false;

        StrategistController strategist = CurrentTarget.GetComponent<StrategistController>();
        if (strategist != null && strategist.currentHealth <= 0)
            return false;

        return true;
    }
    #endregion

    #region Combat
    public void TakeDamage(float damage, Transform attacker = null)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"[Guard] Took {damage} damage! Health: {currentHealth}/{maxHealth}");

        if (attacker != null && attacker.gameObject.activeInHierarchy)
        {
            CurrentTarget = attacker;
            LastKnownPosition = attacker.position;
            ChangeState(new ChaseState(this));
            Debug.Log($"<color=orange>[Guard] Reacting to damage! Switching to Chase!</color>");
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("<color=red>[Guard] Died!</color>");

        StrategistController[] strategists = FindObjectsByType<StrategistController>(FindObjectsSortMode.None);
        foreach (StrategistController strategist in strategists)
        {
            if (strategist.CurrentEnemy == this.transform)
            {
                strategist.CurrentEnemy = null;
                Debug.Log("[Guard] Cleared from Strategist's target list");
            }
        }

        gameObject.SetActive(false);
    }
    #endregion

    #region Debug Visualization
    void OnDrawGizmosSelected()
    {
        // Detection radius (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Attack range (red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Field of view cone (blue)
        Vector3 leftBoundary = Quaternion.Euler(0, -fieldOfViewAngle / 2, 0) * transform.forward * detectionRadius;
        Vector3 rightBoundary = Quaternion.Euler(0, fieldOfViewAngle / 2, 0) * transform.forward * detectionRadius;

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);

        // Current path (green)
        if (agent != null && agent.hasPath)
        {
            Gizmos.color = Color.green;
            Vector3[] corners = agent.path.corners;

            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
        }

        // Line to current target (red)
        if (CurrentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, CurrentTarget.position);
        }
    }
    #endregion
}