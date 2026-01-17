using UnityEngine;

public class AmmoBox : MonoBehaviour
{

    public int ammoAmount = 30;
    public bool isAvailable = true;
    public float respawnTime = 15f;

    private Renderer meshRenderer;
    private Collider col;

    void Start()
    {
        meshRenderer = GetComponent<Renderer>();
        col = GetComponent<Collider>();
    }

    public void collect()
    {
        if (!isAvailable) return;
        isAvailable = false;

        meshRenderer.enabled = false;
        col.enabled = false;

        Invoke("Respawn", respawnTime);

        Debug.Log("Ammo collected Respawning in " + respawnTime + " seconds");
    }

    void Respawn()
    {
        isAvailable = true;
        meshRenderer.enabled = true;
        col.enabled = true;
    }
    void OnDrawGizmos()
    {
        if (isAvailable)
        {
            Gizmos.color = Color.blue;
        }
        else
        {
            Gizmos.color = Color.gray;
        }
        Gizmos.DrawCube(transform.position, Vector3.one * 0.5f);
    }
}
