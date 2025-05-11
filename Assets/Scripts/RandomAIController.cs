using UnityEngine;
using System;
using System.IO;

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
    private Rigidbody rb;
    private Vector2 currentDirection;
    private float lastDirectionChangeTime;
    private float lastShootTime;
    private StreamWriter logFile;

    private void Start()
    {
        try
        {
            string logPath = Path.Combine(Application.dataPath, "../training/build/logs");
            Directory.CreateDirectory(logPath);
            string logFilePath = Path.Combine(logPath, $"random_ai_{gameObject.name}_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            logFile = new StreamWriter(logFilePath, true);
            Log($"[{gameObject.name}] Initializing AI Controller");
            startPosition = transform.position;
            Log($"[{gameObject.name}] Start Position: {startPosition}");
            
            nextDirectionChange = Time.time + changeDirectionInterval;
            nextShootTime = Time.time + shootInterval;
            
            shootingComponent = GetComponent<AIShooting>();
            if (shootingComponent == null)
            {
                Log($"[{gameObject.name}] AIShooting component not found!");
                return;
            }
            Log($"[{gameObject.name}] AIShooting component found");
            
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            }
            
            SetNewRandomTarget();
        }
        catch (Exception e)
        {
            Debug.LogError($"[RandomAIController] Error in Start: {e.Message}\n{e.StackTrace}");
        }
    }

    private void Update()
    {
        if (Time.time - lastDirectionChangeTime > changeDirectionInterval)
        {
            Log($"[{gameObject.name}] Changing direction. Current position: {transform.position}");
            ChangeDirection();
            lastDirectionChangeTime = Time.time;
        }
        
        Move();
        
        if (Time.time - lastShootTime > shootInterval)
        {
            Log($"[{gameObject.name}] Shooting!");
            Shoot();
            lastShootTime = Time.time;
        }
    }

    private void Move()
    {
        if (rb != null)
        {
            Vector3 movement = new Vector3(currentDirection.x, 0, currentDirection.y).normalized * moveSpeed;
            rb.linearVelocity = movement;
            Log($"[{gameObject.name}] Moving: {movement.magnitude:F2} units. Position: {transform.position}");
        }
    }

    private void ChangeDirection()
    {
        currentDirection = new Vector2(
            UnityEngine.Random.Range(-1f, 1f),
            UnityEngine.Random.Range(-1f, 1f)
        ).normalized;
        
        targetPosition = transform.position + new Vector3(currentDirection.x, 0, currentDirection.y) * changeDirectionInterval;
        Log($"[{gameObject.name}] New target position: {targetPosition}");
    }

    private void Shoot()
    {
        if (shootingComponent != null)
        {
            shootingComponent.Shoot();
        }
    }

    private void SetNewRandomTarget()
    {
        // Generate random position within map boundaries
        float randomX = UnityEngine.Random.Range(-MAP_WIDTH, MAP_WIDTH);
        float randomZ = UnityEngine.Random.Range(-MAP_LENGTH, MAP_LENGTH);
        targetPosition = new Vector3(randomX, transform.position.y, randomZ);
        Log($"[{gameObject.name}] New target position: {targetPosition}");
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

    private void OnDestroy()
    {
        logFile?.Close();
        logFile = null;
    }

    private void Log(string message)
    {
        try
        {
            if (logFile == null)
            {
                Debug.LogError($"[RandomAIController] logFile is null! Message: {message}");
                return;
            }
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string goName = "null_gameObject";
            try
            {
                if (gameObject != null)
                    goName = gameObject.name;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RandomAIController] Exception accessing gameObject.name: {ex.Message}");
            }
            string logMessage = $"[{timestamp}] [{goName}] {message}";
            try
            {
                logFile.WriteLine(logMessage);
                logFile.Flush();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RandomAIController] Exception writing to logFile: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RandomAIController] Exception in Log (outer catch): {ex.Message}\n{ex.StackTrace}");
        }
    }
} 