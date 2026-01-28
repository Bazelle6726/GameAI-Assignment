using UnityEngine;

/// <summary>
/// Trigger-based health pickup for agents to collect
/// When an agent touches this, it heals and gets a reward
/// </summary>
public class HealthPickupTrigger : MonoBehaviour
{
    [SerializeField] public float healthAmount = 20f;
    [SerializeField] private float respawnTime = 10f;
    
    private bool isAvailable = true;
    private float respawnTimer = 0f;
    private Material originalMaterial;
    private Material disabledMaterial;

    private void Start()
    {
        // Store original material for visibility toggle
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            originalMaterial = renderer.material;
        }
    }

    private void Update()
    {
        if (!isAvailable)
        {
            respawnTimer -= Time.deltaTime;
            if (respawnTimer <= 0)
            {
                Respawn();
            }
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (!isAvailable) return;
        
        GladiatorAgent agent = collision.GetComponent<GladiatorAgent>();
        if (agent != null && agent.IsAlive)
        {
            // Give reward to agent
            agent.CollectResource(healthAmount);
            
            // Disable pickup until respawn
            Disable();
        }
    }

    private void Disable()
    {
        isAvailable = false;
        respawnTimer = respawnTime;
        
        // Visual feedback: hide or disable the pickup
        gameObject.SetActive(false);
    }

    private void Respawn()
    {
        isAvailable = true;
        respawnTimer = 0f;
        
        // Visual feedback: show the pickup again
        gameObject.SetActive(true);
    }
}
