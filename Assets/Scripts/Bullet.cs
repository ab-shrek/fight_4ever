using UnityEngine;
using System;
using System.IO;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float damage = 10f;
    public GameObject shooter;
    private string instanceId;

    private Vector3 startPosition;
    private Vector3 direction;
    private const float MAX_DISTANCE = 100f;
    private StreamWriter logFile;

    public void SetLogFile(StreamWriter logFile)
    {
        this.logFile = logFile;
        instanceId = System.Environment.GetEnvironmentVariable("INSTANCE_ID") ?? "unknown";
        if (logFile == null)
        {
            Debug.LogError($"[Bullet {gameObject.GetInstanceID()}] Warning: Log file is null");
        }
        else
        {
            Log($"[Bullet {gameObject.GetInstanceID()}] Created at position {transform.position}");
        }
    }

    private void Start()
    {
        startPosition = transform.position;
        direction = transform.forward;
        
        // Add collider if it doesn't exist
        if (GetComponent<SphereCollider>() == null)
        {
            SphereCollider collider = gameObject.AddComponent<SphereCollider>();
            collider.radius = 0.2f;
            collider.isTrigger = true;
        }

        // Log warning if no log file is set
        if (logFile == null)
        {
            Debug.LogWarning($"[Bullet {gameObject.GetInstanceID()}] No log file set. Bullet events will not be logged.");
        }
    }

    private void Log(string message)
    {
        if (logFile != null)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string logMessage = $"[{timestamp}] [Instance:{instanceId}] {message}";
                logFile.WriteLine(logMessage);
                logFile.Flush();
            }
            catch (Exception e)
            {
                Debug.LogError($"[Bullet] Failed to write to log: {e.Message}");
            }
        }
    }

    private void Update()
    {
        // Move the bullet
        float moveDistance = speed * Time.deltaTime;
        transform.position += direction * moveDistance;

        // Check max distance
        if (Vector3.Distance(startPosition, transform.position) > MAX_DISTANCE)
        {
            Log($"[Bullet {gameObject.GetInstanceID()}] Destroyed due to max distance reached");
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignore collisions with other bullets or the shooter
        if (other.gameObject == shooter || other.GetComponent<Bullet>() != null)
            return;

        // Check if we hit a cover
        if (other.gameObject.name.Contains("Cover"))
        {
            var coverCollider = other.GetComponent<Collider>();
            Log($"[Bullet {gameObject.GetInstanceID()}] Hit cover {other.gameObject.name} at position {transform.position} | Shooter: {(shooter != null ? shooter.name : "Unknown")} | Cover bounds center: {coverCollider.bounds.center}, size: {coverCollider.bounds.size}");
            Destroy(gameObject);
            return;
        }

        Log($"[Bullet {gameObject.GetInstanceID()}] Hit {other.gameObject.name} at position {transform.position} | Shooter: {(shooter != null ? shooter.name : "Unknown")}");
        HandleHit(other.gameObject);
    }

    private void HandleHit(GameObject hitObject)
    {
        // Call SceneBuilder's health system
        SceneBuilder sceneBuilder = FindFirstObjectByType<SceneBuilder>();
        if (sceneBuilder != null)
        {
            sceneBuilder.OnPlayerHit(hitObject, damage, transform.position);
        }

        // RLAgent reward logic
        RLAgent hitAgent = hitObject.GetComponent<RLAgent>();
        if (hitAgent != null)
        {
            hitAgent.OnHit(damage);
        }

        Log($"[Bullet {gameObject.GetInstanceID()}] Shooter: {(shooter != null ? shooter.name : "Unknown")} hit target: {hitObject.name}");
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        GameObject hitObject = collision.gameObject;
        Log($"[Bullet {gameObject.GetInstanceID()}] Shooter: {(shooter != null ? shooter.name : "Unknown")} hit target: {hitObject.name}");
        Destroy(gameObject);
    }
} 