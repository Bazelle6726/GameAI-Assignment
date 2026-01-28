using UnityEngine;

/// <summary>
/// Simple test script to verify basic movement without ML-Agents
/// Attach this temporarily to test if Rigidbody and input work
/// </summary>
public class GladiatorMovementTest : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 180f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Debug.Log($"[MovementTest] Start - Rigidbody found: {rb != null}");
        Debug.Log($"[MovementTest] Rigidbody gravity enabled: {rb.useGravity}");
        Debug.Log($"[MovementTest] Rigidbody isKinematic: {rb.isKinematic}");
    }

    void Update()
    {
        // Get input
        float moveInput = Input.GetKey(KeyCode.W) ? 1f : (Input.GetKey(KeyCode.S) ? -1f : 0f);
        float turnInput = Input.GetKey(KeyCode.D) ? 1f : (Input.GetKey(KeyCode.A) ? -1f : 0f);

        // Log input
        if (moveInput != 0 || turnInput != 0)
        {
            Debug.Log($"[MovementTest] Input - Move: {moveInput}, Turn: {turnInput}");
        }

        // Apply movement
        if (moveInput != 0)
        {
            Vector3 moveDirection = transform.forward * moveInput * moveSpeed;
            rb.linearVelocity = new Vector3(moveDirection.x, rb.linearVelocity.y, moveDirection.z);
            Debug.Log($"[MovementTest] Moving! Velocity: {rb.linearVelocity}");
        }

        // Apply rotation
        if (turnInput != 0)
        {
            float rotationDelta = turnInput * rotationSpeed * Time.deltaTime;
            transform.Rotate(0, rotationDelta, 0);
            Debug.Log($"[MovementTest] Rotating! Rotation: {rotationDelta}");
        }

        // Attack key
        if (Input.GetKey(KeyCode.Space))
        {
            Debug.Log("[MovementTest] Space pressed!");
        }
    }
}