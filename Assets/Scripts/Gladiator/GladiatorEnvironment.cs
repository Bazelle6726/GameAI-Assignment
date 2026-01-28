using UnityEngine;
using Unity.MLAgents;
using System.Collections.Generic;

/// <summary>
/// Manages the Battle Arena environment for Gladiator agents
/// Handles:
/// - Episode management (resetting agents when they die)
/// - Reward distribution (kills, survival)
/// - Arena boundaries and spawn points
/// - Resource spawning
/// </summary>
public class GladiatorEnvironment : MonoBehaviour
{
    [Header("Arena Settings")]
    [SerializeField] private Vector3 arenaCenter = Vector3.zero;
    [SerializeField] private Vector3 arenaSize = new Vector3(50f, 10f, 50f);
    [SerializeField] private float groundHeight = 0f;
    
    [Header("Agent Settings")]
    [SerializeField] private int agentsPerMatch = 3;
    [SerializeField] private float agentSpawnHeight = 1f;
    
    [Header("Episode Settings")]
    [SerializeField] private int maxStepsPerEpisode = 5000;
    [SerializeField] private float episodeTimeoutSeconds = 300f; // 5 minutes max per episode
    
    [Header("Resource Settings")]
    [SerializeField] private int healthPickupCount = 5;
    [SerializeField] private float healthPickupAmount = 20f;
    [SerializeField] private GameObject healthPickupPrefab;
    
    // Internal state
    private List<GladiatorAgent> allAgents = new List<GladiatorAgent>();
    private float episodeStartTime;
    private SimpleMultiAgentGroup multiAgentGroup;
    private List<Vector3> spawnPoints = new List<Vector3>();

    private void Start()
    {
        // Initialize multi-agent group for coordinated training
        multiAgentGroup = new SimpleMultiAgentGroup();
        
        // Find all agents in this environment
        GladiatorAgent[] agents = GetComponentsInChildren<GladiatorAgent>();
        foreach (GladiatorAgent agent in agents)
        {
            allAgents.Add(agent);
            multiAgentGroup.RegisterAgent(agent);
        }
        
        // Setup spawn points
        GenerateSpawnPoints();
        
        // Spawn health pickups
        SpawnHealthPickups();
        
        // Start first episode
        episodeStartTime = Time.time;
    }

    private void Update()
    {
        // Check if episode should end
        CheckEpisodeEnd();
    }

    private void GenerateSpawnPoints()
    {
        spawnPoints.Clear();
        
        // Create spawn points in a circle around arena center
        float spawnRadius = Mathf.Min(arenaSize.x, arenaSize.z) / 3f;
        
        for (int i = 0; i < agentsPerMatch; i++)
        {
            float angle = (i / (float)agentsPerMatch) * 360f * Mathf.Deg2Rad;
            float x = arenaCenter.x + Mathf.Cos(angle) * spawnRadius;
            float z = arenaCenter.z + Mathf.Sin(angle) * spawnRadius;
            
            spawnPoints.Add(new Vector3(x, arenaCenter.y + agentSpawnHeight, z));
        }
    }

    public Vector3 GetRandomSpawnPosition()
    {
        if (spawnPoints.Count == 0)
        {
            // Fallback: random position in arena
            float x = Random.Range(arenaCenter.x - arenaSize.x / 2, arenaCenter.x + arenaSize.x / 2);
            float z = Random.Range(arenaCenter.z - arenaSize.z / 2, arenaCenter.z + arenaSize.z / 2);
            return new Vector3(x, arenaCenter.y + agentSpawnHeight, z);
        }
        
        return spawnPoints[Random.Range(0, spawnPoints.Count)];
    }

    private void CheckEpisodeEnd()
    {
        // Count alive agents
        int aliveCount = 0;
        GladiatorAgent survivor = null;
        
        foreach (GladiatorAgent agent in allAgents)
        {
            if (agent.IsAlive)
            {
                aliveCount++;
                survivor = agent;
            }
        }
        
        // Episode ends when:
        // 1. Only one agent left (or all dead)
        // 2. Time limit exceeded
        
        bool shouldEnd = false;
        
        if (aliveCount <= 1)
        {
            // Clear winner (or draw if all dead)
            if (survivor != null)
            {
                survivor.OnEnemyEliminated(); // Reward for winning
            }
            shouldEnd = true;
        }
        
        if (Time.time - episodeStartTime > episodeTimeoutSeconds)
        {
            // Time limit reached
            shouldEnd = true;
        }
        
        if (shouldEnd)
        {
            // End episode for all agents
            multiAgentGroup.GroupEpisodeInterrupted();
            episodeStartTime = Time.time;
        }
    }

    private void SpawnHealthPickups()
    {
        if (healthPickupPrefab == null)
        {
            Debug.LogWarning("Health pickup prefab not assigned in GladiatorEnvironment!");
            return;
        }
        
        for (int i = 0; i < healthPickupCount; i++)
        {
            Vector3 randomPos = GetRandomSpawnPosition();
            randomPos.y = arenaCenter.y + 0.5f; // Slightly above ground
            
            GameObject pickup = Instantiate(healthPickupPrefab, randomPos, Quaternion.identity, transform);
            pickup.name = $"HealthPickup_{i}";
            
            // Add a trigger collider for pickup
            if (pickup.GetComponent<Collider>() == null)
            {
                SphereCollider collider = pickup.AddComponent<SphereCollider>();
                collider.isTrigger = true;
                collider.radius = 0.5f;
            }
            
            // Add pickup script
            HealthPickupTrigger pickupTrigger = pickup.AddComponent<HealthPickupTrigger>();
            pickupTrigger.healthAmount = healthPickupAmount;
        }
    }

    public void ReportAgentDeath(GladiatorAgent deadAgent, GladiatorAgent killer)
    {
        if (killer != null)
        {
            killer.OnEnemyEliminated();
        }
    }

    // Visualization
    private void OnDrawGizmosSelected()
    {
        // Draw arena boundary
        Gizmos.color = Color.cyan;
        Vector3 min = arenaCenter - arenaSize / 2;
        Vector3 max = arenaCenter + arenaSize / 2;
        
        // Draw box outline
        Gizmos.DrawLine(new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z));
        Gizmos.DrawLine(new Vector3(max.x, min.y, min.z), new Vector3(max.x, min.y, max.z));
        Gizmos.DrawLine(new Vector3(max.x, min.y, max.z), new Vector3(min.x, min.y, max.z));
        Gizmos.DrawLine(new Vector3(min.x, min.y, max.z), new Vector3(min.x, min.y, min.z));
    }
}
