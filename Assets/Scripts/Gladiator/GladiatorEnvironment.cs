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
/// 
/// IMPORTANT: Expects GameObjects with names "GladiatorSpawnPoint" in the scene
/// They will be automatically discovered and used for agent spawning
/// </summary>
public class GladiatorEnvironment : MonoBehaviour
{
    [Header("Arena Settings")]
    [SerializeField] private Vector3 arenaCenter = Vector3.zero;
    [SerializeField] private Vector3 arenaSize = new Vector3(50f, 10f, 50f);

    [Header("Agent Settings")]
    [SerializeField] private int agentsPerMatch = 3;
    [SerializeField] private float agentSpawnHeight = 1f;

    [Header("Episode Settings")]
    [SerializeField] private float episodeTimeoutSeconds = 300f; // 5 minutes max per episode

    // Internal state
    private List<GladiatorAgent> allAgents = new List<GladiatorAgent>();
    private List<Transform> spawnPoints = new List<Transform>();
    private float episodeStartTime;
    private SimpleMultiAgentGroup multiAgentGroup;

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

        // Find all spawn points named "GladiatorSpawnPoint"
        FindSpawnPoints();

        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("No GladiatorSpawnPoint GameObjects found in scene! Generating spawn points automatically.");
            GenerateSpawnPointsAutomatically();
        }
        else
        {
            Debug.Log($"Found {spawnPoints.Count} spawn points for Gladiators.");
        }

        // Start first episode
        episodeStartTime = Time.time;
    }

    private void Update()
    {
        // Check if episode should end
        CheckEpisodeEnd();
    }

    /// <summary>
    /// Find all GameObjects named "GladiatorSpawnPoint" in the scene
    /// </summary>
    private void FindSpawnPoints()
    {
        spawnPoints.Clear();

        // Find all objects named "GladiatorSpawnPoint"
        foreach (GameObject obj in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (obj.name.Contains("GladiatorSpawnPoint") || obj.name == "GladiatorSpawnPoint")
            {
                spawnPoints.Add(obj.transform);
            }
        }

        // Sort by name for consistent ordering
        spawnPoints.Sort((a, b) => a.gameObject.name.CompareTo(b.gameObject.name));

        Debug.Log($"Discovered spawn points: {string.Join(", ", spawnPoints.ConvertAll(sp => sp.gameObject.name))}");
    }

    /// <summary>
    /// Automatically generates spawn points in a circle (fallback if none found)
    /// </summary>
    private void GenerateSpawnPointsAutomatically()
    {
        spawnPoints.Clear();

        // Create spawn points in a circle around arena center
        float spawnRadius = Mathf.Min(arenaSize.x, arenaSize.z) / 3f;

        for (int i = 0; i < agentsPerMatch; i++)
        {
            // Create empty GameObject as spawn point
            GameObject spawnPointObj = new GameObject($"GeneratedSpawnPoint_{i}");
            spawnPointObj.transform.SetParent(transform);

            float angle = (i / (float)agentsPerMatch) * 360f * Mathf.Deg2Rad;
            float x = arenaCenter.x + Mathf.Cos(angle) * spawnRadius;
            float z = arenaCenter.z + Mathf.Sin(angle) * spawnRadius;

            spawnPointObj.transform.position = new Vector3(x, arenaCenter.y + agentSpawnHeight, z);
            spawnPoints.Add(spawnPointObj.transform);

            Debug.Log($"Generated spawn point {i} at {spawnPointObj.transform.position}");
        }
    }

    /// <summary>
    /// Get spawn position for a specific agent index
    /// Cycles through spawn points if there are fewer than agents
    /// </summary>
    public Vector3 GetSpawnPosition(int agentIndex)
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("No spawn points available! Returning fallback position.");
            return new Vector3(0, arenaCenter.y + agentSpawnHeight, 0);
        }

        // Cycle through spawn points
        int spawnIndex = agentIndex % spawnPoints.Count;
        return spawnPoints[spawnIndex].position;
    }

    /// <summary>
    /// Get a random spawn position
    /// </summary>
    public Vector3 GetRandomSpawnPosition()
    {
        if (spawnPoints.Count == 0)
        {
            float x = Random.Range(arenaCenter.x - arenaSize.x / 2, arenaCenter.x + arenaSize.x / 2);
            float z = Random.Range(arenaCenter.z - arenaSize.z / 2, arenaCenter.z + arenaSize.z / 2);
            return new Vector3(x, arenaCenter.y + agentSpawnHeight, z);
        }

        int randomIndex = Random.Range(0, spawnPoints.Count);
        return spawnPoints[randomIndex].position;
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

        // Draw spawn points if available
        if (spawnPoints.Count > 0)
        {
            Gizmos.color = Color.green;
            foreach (Transform spawnPoint in spawnPoints)
            {
                if (spawnPoint != null)
                {
                    Gizmos.DrawWireSphere(spawnPoint.position, 1f);
                }
            }
        }
    }
}