using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;

public class GladiatorAgent : Agent
{
    [Header("Combat Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float attackCooldown = 0.5f;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 180f;

    [Header("Detection Settings")]
    [SerializeField] private float detectionRange = 20f;
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private LayerMask resourceMask;

    [Header("Reward Settings")]
    [SerializeField] private float damageGivenReward = 0.1f;
    [SerializeField] private float damageTakenPenalty = -0.05f;
    [SerializeField] private float killReward = 1f;
    [SerializeField] private float survivalReward = 0.001f;
    [SerializeField] private float resourceReward = 0.2f;

    private Rigidbody rb;
    private float currentHealth;
    private float lastAttackTime;
    private List<GladiatorAgent> nearbyEnemies = new List<GladiatorAgent>();
    private GladiatorEnvironment environment;
    private float damageGivenThisStep = 0f;
    private int agentIndex = -1;

    public float CurrentHealth => currentHealth;
    public bool IsAlive => currentHealth > 0;

    public override void Initialize()
    {
        Debug.Log($"[{gameObject.name}] Initialize() called");

        rb = GetComponent<Rigidbody>();
        currentHealth = maxHealth;
        lastAttackTime = -attackCooldown;

        environment = GetComponentInParent<GladiatorEnvironment>();
        if (environment == null)
        {
            Debug.LogError($"GladiatorAgent {gameObject.name} could not find GladiatorEnvironment!");
        }
        else
        {
            Debug.Log($"[{gameObject.name}] Found GladiatorEnvironment");
        }

        agentIndex = transform.GetSiblingIndex();
        Debug.Log($"[{gameObject.name}] Agent index: {agentIndex}");

        if (rb == null)
        {
            Debug.LogError($"[{gameObject.name}] No Rigidbody found!");
            rb = gameObject.AddComponent<Rigidbody>();
        }
        else
        {
            Debug.Log($"[{gameObject.name}] Rigidbody found. Gravity: {!rb.isKinematic}");
        }

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.useGravity = true;
    }

    public override void OnEpisodeBegin()
    {
        Debug.Log($"[{gameObject.name}] Episode Begin");

        currentHealth = maxHealth;
        lastAttackTime = -attackCooldown;
        damageGivenThisStep = 0f;

        if (environment != null)
        {
            Vector3 spawnPos = environment.GetSpawnPosition(agentIndex);
            transform.position = spawnPos;
            Debug.Log($"[{gameObject.name}] Spawned at {spawnPos}");
        }
        else
        {
            transform.position = new Vector3(0, 1f, 0);
            Debug.Log($"[{gameObject.name}] Fallback spawn at (0, 1, 0)");
        }

        transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(currentHealth / maxHealth);
        Vector3 velocity = rb.linearVelocity;
        sensor.AddObservation(velocity.x / moveSpeed);
        sensor.AddObservation(velocity.z / moveSpeed);

        DetectNearbyEnemies();

        for (int i = 0; i < 2; i++)
        {
            if (i < nearbyEnemies.Count)
            {
                GladiatorAgent enemy = nearbyEnemies[i];
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                sensor.AddObservation(distance / detectionRange);

                Vector3 directionToEnemy = (enemy.transform.position - transform.position).normalized;
                sensor.AddObservation(directionToEnemy.x);
                sensor.AddObservation(directionToEnemy.z);
                sensor.AddObservation(enemy.CurrentHealth / maxHealth);
            }
            else
            {
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
            }
        }

        float nearestResourceDistance = FindNearestResourceDistance();
        sensor.AddObservation(nearestResourceDistance / detectionRange);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveForward = actions.ContinuousActions[0];
        float turnDirection = actions.ContinuousActions[1];
        int attackAction = actions.DiscreteActions[0];

        // DEBUG
        if (moveForward != 0 || turnDirection != 0 || attackAction != 0)
        {
            Debug.Log($"[{gameObject.name}] Action - Move: {moveForward:F2}, Turn: {turnDirection:F2}, Attack: {attackAction}");
        }

        ApplyMovement(moveForward, turnDirection);

        if (attackAction == 1)
        {
            AttemptAttack();
        }

        AddReward(survivalReward);

        if (damageGivenThisStep > 0)
        {
            AddReward(damageGivenReward * damageGivenThisStep);
            damageGivenThisStep = 0f;
        }
    }

    private void ApplyMovement(float moveForward, float turnDirection)
    {
        Vector3 moveDirection = transform.forward * moveForward * moveSpeed;
        rb.linearVelocity = new Vector3(moveDirection.x, rb.linearVelocity.y, moveDirection.z);

        float rotationDelta = turnDirection * rotationSpeed * Time.fixedDeltaTime;
        transform.Rotate(0, rotationDelta, 0);
    }

    private void AttemptAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown)
            return;

        lastAttackTime = Time.time;

        Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, attackRange, enemyMask);

        foreach (Collider collision in enemiesInRange)
        {
            GladiatorAgent enemy = collision.GetComponent<GladiatorAgent>();
            if (enemy != null && enemy != this && enemy.IsAlive)
            {
                enemy.TakeDamage(attackDamage);
                damageGivenThisStep += 1f;
                AddReward(damageGivenReward);
                Debug.Log($"[{gameObject.name}] Hit {enemy.gameObject.name}!");
                break;
            }
        }
    }

    public void TakeDamage(float damage)
    {
        if (!IsAlive) return;

        currentHealth -= damage;
        AddReward(damageTakenPenalty);
        Debug.Log($"[{gameObject.name}] Took {damage} damage! Health: {currentHealth}");

        if (!IsAlive)
        {
            Debug.Log($"[{gameObject.name}] DIED!");
            EndEpisode();
        }
    }

    public void CollectResource(float resourceValue)
    {
        currentHealth = Mathf.Min(currentHealth + resourceValue, maxHealth);
        AddReward(resourceReward);
        Debug.Log($"[{gameObject.name}] Collected resource! Health: {currentHealth}");
    }

    public void OnEnemyEliminated()
    {
        AddReward(killReward);
        Debug.Log($"[{gameObject.name}] Enemy eliminated! +{killReward} reward");
    }

    private void DetectNearbyEnemies()
    {
        nearbyEnemies.Clear();

        Collider[] detectedEnemies = Physics.OverlapSphere(transform.position, detectionRange, enemyMask);

        foreach (Collider collision in detectedEnemies)
        {
            GladiatorAgent enemy = collision.GetComponent<GladiatorAgent>();
            if (enemy != null && enemy != this && enemy.IsAlive)
            {
                nearbyEnemies.Add(enemy);
            }
        }

        nearbyEnemies.Sort((a, b) =>
            Vector3.Distance(transform.position, a.transform.position)
            .CompareTo(Vector3.Distance(transform.position, b.transform.position))
        );
    }

    private float FindNearestResourceDistance()
    {
        Collider[] resources = Physics.OverlapSphere(transform.position, detectionRange, resourceMask);

        if (resources.Length == 0)
            return detectionRange;

        float closestDistance = float.MaxValue;
        foreach (Collider resource in resources)
        {
            float distance = Vector3.Distance(transform.position, resource.transform.position);
            if (distance < closestDistance)
                closestDistance = distance;
        }

        return closestDistance;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;

        float moveInput = Input.GetKey(KeyCode.W) ? 1f : (Input.GetKey(KeyCode.S) ? -1f : 0f);
        float turnInput = Input.GetKey(KeyCode.D) ? 1f : (Input.GetKey(KeyCode.A) ? -1f : 0f);

        if (moveInput != 0 || turnInput != 0)
        {
            Debug.Log($"[{gameObject.name}] Heuristic input - Move: {moveInput}, Turn: {turnInput}");
        }

        continuousActions[0] = moveInput;
        continuousActions[1] = turnInput;
        discreteActions[0] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}