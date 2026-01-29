using UnityEngine;
using Unity.MLAgents;
using System.Collections.Generic;

public class GladiatorEnvironment : MonoBehaviour
{
    [Header("Arena Settings")]
    [SerializeField] private Vector3 arenaCenter = Vector3.zero;
    [SerializeField] private Vector3 arenaSize = new Vector3(50f, 10f, 50f);

    [Header("Agent Settings")]
    [SerializeField] private int agentsPerMatch = 3;
    [SerializeField] private float agentSpawnHeight = 1f;

    [Header("Episode Settings")]
    [SerializeField] private float episodeTimeoutSeconds = 300f;

    private List<GladiatorAgent> allAgents = new List<GladiatorAgent>();
    private List<Transform> spawnPoints = new List<Transform>();
    private float episodeStartTime;
    private SimpleMultiAgentGroup multiAgentGroup;

    private void Start()
    {
        multiAgentGroup = new SimpleMultiAgentGroup();

        GladiatorAgent[] agents = GetComponentsInChildren<GladiatorAgent>();
        foreach (GladiatorAgent agent in agents)
        {
            allAgents.Add(agent);
            multiAgentGroup.RegisterAgent(agent);
        }

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

        episodeStartTime = Time.time;
    }

    private void Update()
    {
        CheckEpisodeEnd();
    }

    // Find all GameObjects named "GladiatorSpawnPoint" in the scene
    private void FindSpawnPoints()
    {
        spawnPoints.Clear();

        foreach (GameObject obj in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (obj.name.Contains("GladiatorSpawnPoint") || obj.name == "GladiatorSpawnPoint")
            {
                spawnPoints.Add(obj.transform);
            }
        }

        spawnPoints.Sort((a, b) => a.gameObject.name.CompareTo(b.gameObject.name));
        Debug.Log($"Discovered spawn points: {string.Join(", ", spawnPoints.ConvertAll(sp => sp.gameObject.name))}");
    }

    // Fallback: automatically generate spawn points in a circle
    private void GenerateSpawnPointsAutomatically()
    {
        spawnPoints.Clear();

        float spawnRadius = Mathf.Min(arenaSize.x, arenaSize.z) / 3f;

        for (int i = 0; i < agentsPerMatch; i++)
        {
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

    public Vector3 GetSpawnPosition(int agentIndex)
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("No spawn points available! Returning fallback position.");
            return new Vector3(0, arenaCenter.y + agentSpawnHeight, 0);
        }

        int spawnIndex = agentIndex % spawnPoints.Count;
        return spawnPoints[spawnIndex].position;
    }

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

        bool shouldEnd = false;

        // Episode ends: only one agent left (or all dead)
        if (aliveCount <= 1)
        {
            if (survivor != null)
            {
                survivor.OnEnemyEliminated();
            }
            shouldEnd = true;
        }

        // Episode ends: time limit exceeded
        if (Time.time - episodeStartTime > episodeTimeoutSeconds)
        {
            shouldEnd = true;
        }

        if (shouldEnd)
        {
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

    private void OnDrawGizmosSelected()
    {
        // Draw arena boundary
        Gizmos.color = Color.cyan;
        Vector3 min = arenaCenter - arenaSize / 2;
        Vector3 max = arenaCenter + arenaSize / 2;

        Gizmos.DrawLine(new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z));
        Gizmos.DrawLine(new Vector3(max.x, min.y, min.z), new Vector3(max.x, min.y, max.z));
        Gizmos.DrawLine(new Vector3(max.x, min.y, max.z), new Vector3(min.x, min.y, max.z));
        Gizmos.DrawLine(new Vector3(min.x, min.y, max.z), new Vector3(min.x, min.y, min.z));

        // Draw spawn points
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