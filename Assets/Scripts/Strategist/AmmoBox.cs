using UnityEngine;

public class AmmoBox : MonoBehaviour
{

    public int ammoAmount = 30;
    public bool isAvailable = true;
    public float respawnTime = 15f;

    private Renderer[] childRenderers;
    private Collider col;

    void Start()
    {
        col = GetComponent<Collider>();
        childRenderers = GetComponentsInChildren<Renderer>();
    }

    public void Collect()
    {
        if (!isAvailable)
        {
            Debug.LogWarning("AmmoBox already collected!");
            return;  // Already collected, don't do anything
        }

        isAvailable = false;

        // Hide all child renderers
        foreach (Renderer renderer in childRenderers)
        {
            renderer.enabled = false;
        }

        // Disable collider
        if (col != null)
        {
            col.enabled = false;
        }

        Invoke("Respawn", respawnTime);

        Debug.Log("Ammo collected Respawning in " + respawnTime + " seconds");
    }

    void Respawn()
    {
        foreach (Renderer renderer in childRenderers)
        {
            renderer.enabled = true;
        }

        if (col != null)
        {
            col.enabled = true;
        }
        Debug.Log("Ammo box respawned");
    }

    void OnDrawGizmos()
    {
        if (isAvailable)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}
