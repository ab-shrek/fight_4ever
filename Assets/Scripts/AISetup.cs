using UnityEngine;

public class AISetup : MonoBehaviour
{
    [SerializeField] private float bulletSpeed = 20f;
    [SerializeField] private float fireRate = 1f;
    [SerializeField] private float bulletLifetime = 3f;
    [SerializeField] private float bulletDamage = 10f;
    [SerializeField] private Vector3 firePointOffset = new Vector3(0, 1, 1);

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

        // Add RLAgent for ML-Agents
        RLAgent rlAgent = aiCharacter.AddComponent<RLAgent>();

        // Add and configure BehaviorParameters programmatically
        var behaviorParams = aiCharacter.AddComponent<Unity.MLAgents.Policies.BehaviorParameters>();
        behaviorParams.BehaviorName = "RLAgent";
        behaviorParams.BrainParameters.ActionSpec = Unity.MLAgents.Actuators.ActionSpec.MakeContinuous(3); // moveX, moveZ, shoot
        behaviorParams.BehaviorType = Unity.MLAgents.Policies.BehaviorType.Default;

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

        // Set up the shooting parameters through reflection
        var shootingType = typeof(AIShooting);
        var bulletSpeedField = shootingType.GetField("bulletSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var fireRateField = shootingType.GetField("fireRate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var bulletLifetimeField = shootingType.GetField("bulletLifetime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var bulletDamageField = shootingType.GetField("bulletDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var firePointOffsetField = shootingType.GetField("firePointOffset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        bulletSpeedField?.SetValue(shootingComponent, bulletSpeed);
        fireRateField?.SetValue(shootingComponent, fireRate);
        bulletLifetimeField?.SetValue(shootingComponent, bulletLifetime);
        bulletDamageField?.SetValue(shootingComponent, bulletDamage);
        firePointOffsetField?.SetValue(shootingComponent, firePointOffset);
    }
} 