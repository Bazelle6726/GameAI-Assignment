using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;

public class StrategistController : MonoBehaviour
{
    [Header("Components")]
    public NavMeshAgent agent;

    [Header("Stats")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public int maxAmmo = 90;
    public int currentAmmo = 90;

    [Header("Combat Settings")]
    public float attackRange = 8f;
    public float attackDamage = 10f;
    public float attackCooldown = 1f;
    public float lastAttackTime = 0f;

    [Header("detection Settings")]
    public float detectionRadius = 15f;
    public LayerMask enemyLayer;
    public LayerMask obstacleLayer;

    [Header("Movement Settings")]
    public float normalSpeed = 4f;
    public float fleeSpeed = 6f;

    [Header("Utility AI Settings")]
    public float actionEvaluationInterval = 0.2f;
    private float evaluationTimer = 0f;


    private List<UtilityAction> availableActions;
    private UtilityAction currentAction;

    public Transform CurrentEnemy { get; set; }
    public Firstaid TargetFirstaid { get; set; }
    public AmmoBox TargetAmmoBox { get; set; }

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = normalSpeed;

        InitializeActions();

        Debug.Log($"Strategist initialized with {availableActions.Count} actions");
    }

    void InitializeActions()
    {
        availableActions = new List<UtilityAction>
        {
            new HealAction(this),
            new CollectAmmoAction(this),
            new HideAction(this),
            new AttackAction(this),
            new IdleAction(this)
        };
    }

    void Update()
    {
        // Update health display (clamped)
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        currentAmmo = Mathf.Clamp(currentAmmo, 0, maxAmmo);

        // Detect threats
        DetectEnemy();

        // Evaluate actions at intervals
        evaluationTimer += Time.deltaTime;
        if (evaluationTimer >= actionEvaluationInterval)
        {
            evaluationTimer = 0f;
            EvaluateAndSelectAction();
        }

        // Execute current action
        currentAction?.Execute();
    }


    void EvaluateAndSelectAction()
    {
        float highestUtility = 0f;
        UtilityAction bestAction = null;

        // Calculate utility for each action
        foreach (UtilityAction action in availableActions)
        {
            float utility = action.CalculateUtility();

            if (utility > highestUtility)
            {
                highestUtility = utility;
                bestAction = action;
            }
        }

        // Switch to best action if it changed
        if (bestAction != currentAction && bestAction != null)
        {
            currentAction = bestAction;
            Debug.Log($"<color=cyan>Strategist switching to: {currentAction.GetActionName()} (utility: {highestUtility:F2})</color>");
        }
    }


    void DetectEnemy()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, detectionRadius, enemyLayer);

        if (enemies.Length > 0)
        {
            // Find closest threat
            Transform closestEnemy = null;
            float closestDistance = Mathf.Infinity;

            foreach (Collider enemy in enemies)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = enemy.transform;
                }
            }

            CurrentEnemy = closestEnemy;
        }
        else
        {
            CurrentEnemy = null;
        }
    }


    public Firstaid FindNearestFirstaid()
    {
        Firstaid[] firstAids = FindObjectsByType<Firstaid>(FindObjectsSortMode.None);
        Firstaid nearest = null;
        float closestDistance = Mathf.Infinity;

        foreach (Firstaid firstaid in firstAids)
        {
            if (!firstaid.isAvailable) continue;

            float distance = Vector3.Distance(transform.position, firstaid.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                nearest = firstaid;
            }
        }

        return nearest;
    }


    public AmmoBox FindNearestAmmoBox()
    {
        AmmoBox[] ammoBoxes = FindObjectsByType<AmmoBox>(FindObjectsSortMode.None);
        AmmoBox nearest = null;
        float closestDistance = Mathf.Infinity;

        foreach (AmmoBox crate in ammoBoxes)
        {
            if (!crate.isAvailable) continue;

            float distance = Vector3.Distance(transform.position, crate.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                nearest = crate;
            }
        }

        return nearest;
    }


    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log($"Strategist took {damage} damage! Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }


    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log($"Strategist healed {amount}! Health: {currentHealth}/{maxHealth}");
    }


    public void AddAmmo(int amount)
    {
        currentAmmo = Mathf.Min(currentAmmo + amount, maxAmmo);
        Debug.Log($"Strategist collected {amount} ammo! Ammo: {currentAmmo}/{maxAmmo}");
    }

    void Die()
    {
        Debug.Log("Strategist died!");
        // Could respawn, show game over, etc.
        gameObject.SetActive(false);
    }

    // Visual debugging
    void OnDrawGizmosSelected()
    {
        // Detection radius
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Line to current threat
        if (CurrentEnemy != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, CurrentEnemy.position);
        }

        // Line to target firstaid
        if (TargetFirstaid != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, TargetFirstaid.transform.position);
        }

        // Line to target ammo crate
        if (TargetAmmoBox != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, TargetAmmoBox.transform.position);
        }
    }
}
