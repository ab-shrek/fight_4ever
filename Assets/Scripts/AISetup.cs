using UnityEngine;
using UnityEngine.Rendering;

public class AISetup : MonoBehaviour
{
    [SerializeField] private float bulletSpeed = 20f;
    [SerializeField] private float fireRate = 1f;
    [SerializeField] private float bulletLifetime = 3f;
    [SerializeField] private float bulletDamage = 10f;
    [SerializeField] private Vector3 firePointOffset = new Vector3(0, 1, 1);

    private void Awake()
    {
        // Check if running in headless mode
        if (Application.isBatchMode)
        {
            Debug.Log("Running in headless mode - configuring minimal graphics settings");
            
            // Disable cameras and renderers
            var cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var camera in cameras)
            {
                camera.enabled = false;
            }

            var renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (var renderer in renderers)
            {
                renderer.enabled = false;
            }

            // Set quality level to lowest
            QualitySettings.SetQualityLevel(0);
            
            // Disable shadows
            QualitySettings.shadows = ShadowQuality.Disable;
            
            // Disable anti-aliasing
            QualitySettings.antiAliasing = 0;
            
            // Disable vsync
            QualitySettings.vSyncCount = 0;
            
            // Set target frame rate
            Application.targetFrameRate = 60;
        }
    }

    private void Start()
    {
        SetupAI();
    }

    public static GameObject CreateAICharacter(Vector3 position, Quaternion rotation, string name = "AICharacter")
    {
        // Create the AI GameObject
        GameObject aiCharacter = new GameObject(name);
        aiCharacter.transform.position = position;
        aiCharacter.transform.rotation = rotation;

        // Add a visual representation (cube)
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.transform.SetParent(aiCharacter.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = new Vector3(1, 2, 1); // Make it look like a character

        // Add Rigidbody
        Rigidbody rb = aiCharacter.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation; // Prevent tipping

        // Add Collider
        BoxCollider collider = aiCharacter.AddComponent<BoxCollider>();
        collider.size = new Vector3(1, 2, 1);
        collider.center = new Vector3(0, 1, 0);

        // Add HealthSystem
        HealthSystem healthSystem = aiCharacter.AddComponent<HealthSystem>();

        // Add AISetup and configure it
        AISetup setup = aiCharacter.AddComponent<AISetup>();
        setup.bulletSpeed = 20f;
        setup.fireRate = 1f;
        setup.bulletLifetime = 3f;
        setup.bulletDamage = 10f;
        setup.firePointOffset = new Vector3(0, 1, 1);

        // Setup the AI
        setup.SetupAI();

        // Add RLAgent for Barracuda-based RL
        RLAgent rlAgent = aiCharacter.AddComponent<RLAgent>();

        return aiCharacter;
    }

    public void SetupAI()
    {
        // Add AIShooting component if it doesn't exist
        AIShooting shootingComponent = GetComponent<AIShooting>();
        if (shootingComponent == null)
        {
            shootingComponent = gameObject.AddComponent<AIShooting>();
        }

        // Load bullet prefab from Resources
        GameObject bulletPrefab = Resources.Load<GameObject>("Bullet");
        if (bulletPrefab == null)
        {
            Debug.LogError("Bullet prefab not found in Resources folder!");
            return;
        }

        // Create fire point
        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(transform);
        firePoint.transform.localPosition = firePointOffset;
        firePoint.transform.localRotation = Quaternion.identity;

        // Set up the shooting parameters through reflection
        var shootingType = typeof(AIShooting);
        var bulletPrefabField = shootingType.GetField("bulletPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var firePointField = shootingType.GetField("firePoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var bulletSpeedField = shootingType.GetField("bulletSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var fireRateField = shootingType.GetField("fireRate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var bulletLifetimeField = shootingType.GetField("bulletLifetime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var bulletDamageField = shootingType.GetField("bulletDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var firePointOffsetField = shootingType.GetField("firePointOffset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        bulletPrefabField?.SetValue(shootingComponent, bulletPrefab);
        firePointField?.SetValue(shootingComponent, firePoint.transform);
        bulletSpeedField?.SetValue(shootingComponent, bulletSpeed);
        fireRateField?.SetValue(shootingComponent, fireRate);
        bulletLifetimeField?.SetValue(shootingComponent, bulletLifetime);
        bulletDamageField?.SetValue(shootingComponent, bulletDamage);
        firePointOffsetField?.SetValue(shootingComponent, firePointOffset);

        Debug.Log($"Set up AIShooting for {gameObject.name} with bullet prefab and fire point");
    }
} 