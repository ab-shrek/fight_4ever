using UnityEngine;

public class RandomAIController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float changeDirectionInterval = 2f;
    [SerializeField] private float shootInterval = 1f;

    // Map boundaries (based on ground size 30x20)
    private const float MAP_WIDTH = 15f;  // Half of 30
    private const float MAP_LENGTH = 10f; // Half of 20

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float nextDirectionChange;
    private float nextShootTime;
    private AIShooting shootingComponent;

    private void Start()
    {
        Debug.Log($"[{gameObject.name}] Initializing AI Controller");
        startPosition = transform.position;
        Debug.Log($"[{gameObject.name}] Start Position: {startPosition}");
        
        nextDirectionChange = Time.time + changeDirectionInterval;
        nextShootTime = Time.time + shootInterval;
        
        shootingComponent = GetComponent<AIShooting>();
        if (shootingComponent == null)
        {
            Debug.LogError($"[{gameObject.name}] AIShooting component not found!");
        }
        else
        {
            Debug.Log($"[{gameObject.name}] AIShooting component found");
        }
        
        SetNewRandomTarget();
    }

    private void Update()
    {
        // Random movement
        if (Time.time >= nextDirectionChange)
        {
            Debug.Log($"[{gameObject.name}] Changing direction. Current position: {transform.position}");
            SetNewRandomTarget();
            nextDirectionChange = Time.time + changeDirectionInterval;
        }

        // Move towards target
        Vector3 direction = (targetPosition - transform.position).normalized;
        Vector3 movement = direction * moveSpeed * Time.deltaTime;
        
        // Calculate new position
        Vector3 newPosition = transform.position + movement;
        
        // Clamp position within map boundaries
        newPosition.x = Mathf.Clamp(newPosition.x, -MAP_WIDTH, MAP_WIDTH);
        newPosition.z = Mathf.Clamp(newPosition.z, -MAP_LENGTH, MAP_LENGTH);
        
        // Apply movement
        transform.position = newPosition;
        
        // Debug movement
        if (movement.magnitude > 0.01f)
        {
            Debug.Log($"[{gameObject.name}] Moving: {movement.magnitude:F2} units. Position: {transform.position}");
        }

        // Look in movement direction
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        // Random shooting
        if (Time.time >= nextShootTime)
        {
            if (shootingComponent != null)
            {
                Debug.Log($"[{gameObject.name}] Shooting!");
                shootingComponent.Shoot();
            }
            nextShootTime = Time.time + shootInterval;
        }
    }

    private void SetNewRandomTarget()
    {
        // Generate random position within map boundaries
        float randomX = Random.Range(-MAP_WIDTH, MAP_WIDTH);
        float randomZ = Random.Range(-MAP_LENGTH, MAP_LENGTH);
        targetPosition = new Vector3(randomX, transform.position.y, randomZ);
        Debug.Log($"[{gameObject.name}] New target position: {targetPosition}");
    }

    private void OnDrawGizmos()
    {
        // Draw map boundaries
        Gizmos.color = Color.green;
        Vector3 size = new Vector3(MAP_WIDTH * 2, 0.1f, MAP_LENGTH * 2);
        Gizmos.DrawWireCube(Vector3.zero, size);
        
        // Draw current target
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(targetPosition, 0.5f);
        
        // Draw path to target
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, targetPosition);
    }
} 