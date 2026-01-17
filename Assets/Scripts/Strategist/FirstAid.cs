using UnityEngine;

public class Firstaid : MonoBehaviour
{
    public float healAmount = 25f;
    public bool isAvailable = true;
    public float respawnTime = 10f;

    private Renderer meshRenderer;
    private Collider col;

    void Start()
    {
        meshRenderer = GetComponent<Renderer>();
        col = GetComponent<Collider>();
    }

    public void Collect()
    {
        if (!isAvailable) return;
        isAvailable = false;

        meshRenderer.enabled = false;
        col.enabled = false;

        Invoke("Respawn", respawnTime);
        Debug.Log("First Aid Kit Collected" + respawnTime + " seconds");
    }

    void Respawn()
    {
        isAvailable = true;
        meshRenderer.enabled = true;
        col.enabled = true;

        Debug.Log("First Aid Kit Respawned");
    }

    void OnDrawGizmos()
    {
        if (isAvailable)
        {
            Gizmos.color = Color.green;
        }
        else
        {
            Gizmos.color = Color.red;
        }
        Gizmos.DrawSphere(transform.position, 0.5f);
    }
}
