using UnityEngine;
using UnityEngine.AI;

public class NavMeshTest : MonoBehaviour
{
    private NavMeshAgent agent;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component not found!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                // Move agent to clicked position
                agent.SetDestination(hit.point);
                Debug.Log("Moving to: " + hit.point);
            }
        }
    }
}
