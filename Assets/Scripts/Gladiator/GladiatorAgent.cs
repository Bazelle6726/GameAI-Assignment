using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;

/// <summary>
/// Gladiator ML-Agent for reinforcement learning combat
/// Trained using self-play to compete against other Gladiators
/// 
/// OBSERVATIONS (input to neural network):
/// - Own health (normalized 0-1)
/// - Own velocity (x, z components)
/// - 2 Enemies detected:
///   - Enemy distance (normalized)
///   - Enemy direction (x, z relative)
///   - Enemy health (normalized)
/// - Nearest resource distance
/// - Total: 12 observations
/// 
/// ACTIONS (output from neural network):
/// - Movement: Forward/Backward (continuous)
/// - Turning: Left/Right (continuous)
/// - Attack: Yes/No (discrete)
/// </summary>
public class GladiatorAgent : Agent
{
    [Header("Combat Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float attackCooldown = 0.5f;
    
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 180f; // degrees per second
    
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
    
    // Internal state
    private Rigidbody rb;
    private float currentHealth;
    private float lastAttackTime;
    private List<GladiatorAgent> nearbyEnemies = new List<GladiatorAgent>();
    private GladiatorEnvironment environment;
    
    // For tracking damage given (used in rewards)
    private float damageGivenThisStep = 0f;
    
    public float CurrentHealth => currentHealth;
    public bool IsAlive => currentHealth > 0;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        currentHealth = maxHealth;
        lastAttackTime = -attackCooldown;
        
        // Find the environment manager
        environment = GetComponentInParent<GladiatorEnvironment>();
        if (environment == null)
        {
            Debug.LogError($"GladiatorAgent {gameObject.name} could not find GladiatorEnvironment!");
        }
        
        // Configure Rigidbody
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.useGravity = true;
    }

    public override void OnEpisodeBegin()
    {
        // Reset health and position
        currentHealth = maxHealth;
        lastAttackTime = -attackCooldown;
        damageGivenThisStep = 0f;
        
        // Random spawn position within arena
        if (environment != null)
        {
            transform.position = environment.GetRandomSpawnPosition();
        }
        
        // Reset velocity
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Health observation (normalized 0-1)
        sensor.AddObservation(currentHealth / maxHealth);
        
        // Velocity observations (x and z)
        Vector3 velocity = rb.linearVelocity;
        sensor.AddObservation(velocity.x / moveSpeed);
        sensor.AddObservation(velocity.z / moveSpeed);
        
        // Detect nearby enemies
        DetectNearbyEnemies();
        
        // Add observations for up to 2 enemies
        // If fewer than 2 enemies, pad with zeros
        for (int i = 0; i < 2; i++)
        {
            if (i < nearbyEnemies.Count)
            {
                GladiatorAgent enemy = nearbyEnemies[i];
                
                // Distance to enemy (normalized)
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                sensor.AddObservation(distance / detectionRange);
                
                // Direction to enemy (relative, normalized)
                Vector3 directionToEnemy = (enemy.transform.position - transform.position).normalized;
                sensor.AddObservation(directionToEnemy.x);
                sensor.AddObservation(directionToEnemy.z);
                
                // Enemy health (normalized)
                sensor.AddObservation(enemy.CurrentHealth / maxHealth);
            }
            else
            {
                // Pad with zeros if no enemy in this slot
                sensor.AddObservation(0f); // distance
                sensor.AddObservation(0f); // direction x
                sensor.AddObservation(0f); // direction z
                sensor.AddObservation(0f); // health
            }
        }
        
        // Nearest resource distance (for curriculum learning)
        float nearestResourceDistance = FindNearestResourceDistance();
        sensor.AddObservation(nearestResourceDistance / detectionRange);
        
        // Total observations: 1 (health) + 2 (velocity) + 2*4 (2 enemies) + 1 (resource) = 12
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Get continuous actions
        float moveForward = actions.ContinuousActions[0]; // -1 to 1
        float turnDirection = actions.ContinuousActions[1]; // -1 to 1
        
        // Get discrete action
        int attackAction = actions.DiscreteActions[0]; // 0 = no attack, 1 = attack
        
        // Apply movement
        ApplyMovement(moveForward, turnDirection);
        
        // Apply attack
        if (attackAction == 1)
        {
            AttemptAttack();
        }
        
        // Survival reward (small positive reward each step to encourage staying alive)
        AddReward(survivalReward);
        
        // Reward for damage given this step
        if (damageGivenThisStep > 0)
        {
            AddReward(damageGivenReward * damageGivenThisStep);
            damageGivenThisStep = 0f;
        }
    }

    private void ApplyMovement(float moveForward, float turnDirection)
    {
        // Forward/backward movement
        Vector3 moveDirection = transform.forward * moveForward * moveSpeed;
        
        // Keep current velocity in y axis (gravity)
        rb.linearVelocity = new Vector3(moveDirection.x, rb.linearVelocity.y, moveDirection.z);
        
        // Rotation
        float rotationDelta = turnDirection * rotationSpeed * Time.fixedDeltaTime;
        transform.Rotate(0, rotationDelta, 0);
    }

    private void AttemptAttack()
    {
        // Check cooldown
        if (Time.time - lastAttackTime < attackCooldown)
            return;
        
        lastAttackTime = Time.time;
        
        // Find enemies in attack range
        Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, attackRange, enemyMask);
        
        foreach (Collider collision in enemiesInRange)
        {
            GladiatorAgent enemy = collision.GetComponent<GladiatorAgent>();
            if (enemy != null && enemy != this && enemy.IsAlive)
            {
                // Deal damage
                enemy.TakeDamage(attackDamage);
                damageGivenThisStep += 1f;
                
                // Reward for hitting enemy
                AddReward(damageGivenReward);
                break; // Only hit one enemy per attack
            }
        }
    }

    public void TakeDamage(float damage)
    {
        if (!IsAlive) return;
        
        currentHealth -= damage;
        AddReward(damageTakenPenalty);
        
        if (!IsAlive)
        {
            // Agent died
            EndEpisode();
        }
    }

    public void CollectResource(float resourceValue)
    {
        // Heal from resource
        currentHealth = Mathf.Min(currentHealth + resourceValue, maxHealth);
        AddReward(resourceReward);
    }

    public void OnEnemyEliminated()
    {
        // Called when this agent eliminates an enemy
        AddReward(killReward);
    }

    private void DetectNearbyEnemies()
    {
        nearbyEnemies.Clear();
        
        // Find all enemies within detection range
        Collider[] detectedEnemies = Physics.OverlapSphere(transform.position, detectionRange, enemyMask);
        
        foreach (Collider collision in detectedEnemies)
        {
            GladiatorAgent enemy = collision.GetComponent<GladiatorAgent>();
            if (enemy != null && enemy != this && enemy.IsAlive)
            {
                nearbyEnemies.Add(enemy);
            }
        }
        
        // Sort by distance (closest first)
        nearbyEnemies.Sort((a, b) => 
            Vector3.Distance(transform.position, a.transform.position)
            .CompareTo(Vector3.Distance(transform.position, b.transform.position))
        );
    }

    private float FindNearestResourceDistance()
    {
        // Find nearest health pack or ammo crate
        Collider[] resources = Physics.OverlapSphere(transform.position, detectionRange, resourceMask);
        
        if (resources.Length == 0)
            return detectionRange; // No resources found
        
        float closestDistance = float.MaxValue;
        foreach (Collider resource in resources)
        {
            float distance = Vector3.Distance(transform.position, resource.transform.position);
            if (distance < closestDistance)
                closestDistance = distance;
        }
        
        return closestDistance;
    }

    // Heuristic for manual testing
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
        
        // Move forward/backward with W/S
        continuousActions[0] = Input.GetKey(KeyCode.W) ? 1f : (Input.GetKey(KeyCode.S) ? -1f : 0f);
        
        // Turn left/right with A/D
        continuousActions[1] = Input.GetKey(KeyCode.D) ? 1f : (Input.GetKey(KeyCode.A) ? -1f : 0f);
        
        // Attack with Space
        discreteActions[0] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }

    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
