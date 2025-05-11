using UnityEngine;
using System;
using System.IO;

public class Bullet : MonoBehaviour
{
    [SerializeField] public float speed = 20f;
    [SerializeField] public float damage = 10f;
    public GameObject shooter;
    private string instanceId;
    private Vector3 startPosition;
    private Vector3 direction;
    private const float MAX_DISTANCE = 100f;
    private StreamWriter logFile;

    public void SetLogFile(StreamWriter file)
    {
        logFile = file;
        Log("Log file set");
    }

    private void Start()
    {
        try
        {
            instanceId = System.Environment.GetEnvironmentVariable("INSTANCE_ID") ?? "unknown";
            startPosition = transform.position;
            direction = transform.forward;
            Log($"Created at position {transform.position}");
            
            // Add collider if it doesn't exist
            if (GetComponent<SphereCollider>() == null)
            {
                SphereCollider collider = gameObject.AddComponent<SphereCollider>();
                collider.radius = 0.2f;
                collider.isTrigger = true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Bullet] Error in Start: {e.Message}\n{e.StackTrace}");
        }
    }

    private void Log(string message)
    {
        string fullMessage = $"[Bullet {gameObject.GetInstanceID()}] {message} | Shooter: {(shooter != null ? shooter.name : "Unknown")}";
        if (logFile != null)
        {
            logFile.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {fullMessage}");
            logFile.Flush();
        }
        Debug.Log(fullMessage);
    }

    private void Update()
    {
        // Move the bullet
        float moveDistance = speed * Time.deltaTime;
        transform.position += direction * moveDistance;

        // Log position every second
        if (Time.frameCount % 60 == 0)  // Assuming 60 FPS
        {
            Log($"Current position: {transform.position}, Distance from start: {Vector3.Distance(startPosition, transform.position):F2}");
        }

        // Check max distance
        if (Vector3.Distance(startPosition, transform.position) > MAX_DISTANCE)
        {
            Log("Destroyed due to max distance reached");
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignore collisions with other bullets or the shooter
        if (other.gameObject == shooter || other.GetComponent<Bullet>() != null)
        {
            Log($"Ignoring collision with {(other.gameObject == shooter ? "shooter" : "other bullet")}: {other.gameObject.name}");
            return;
        }

        // Check if we hit a cover
        if (other.gameObject.name.Contains("Cover"))
        {
            var coverCollider = other.GetComponent<Collider>();
            Log($"Hit cover {other.gameObject.name} at position {transform.position} | Cover bounds center: {coverCollider.bounds.center}, size: {coverCollider.bounds.size}");
            Destroy(gameObject);
            return;
        }

        Log($"Hit {other.gameObject.name} at position {transform.position}");
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

        Log($"Hit target: {hitObject.name}");
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        GameObject hitObject = collision.gameObject;
        Log($"Hit target: {hitObject.name}");
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (logFile != null)
        {
            Log("Bullet destroyed");
            logFile.Close();
            logFile = null;
        }
    }
} 