using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using Unity.Barracuda;
using System.Collections;
using UnityEngine.Networking;

public class RLAgent : MonoBehaviour
{
    [SerializeField] private float movementSpeed = 7f;
    [SerializeField] private float explorationReward = 0.01f;
    [SerializeField] private float stagnationPenalty = 0.005f;
    public bool IsPlayerOne { get; set; }

    [Header("Barracuda Model")]
    public NNModel nnModelAsset;

    [Header("Server Communication")]
    public string serverUrl = "http://localhost:5000/get_action"; // Default to player 1 port
    private bool isServerConnected = false;
    private float[] lastAction = new float[3]; // Store last action for debugging
    private int playerId; // Store the player ID
    private float requestTimeout = 1.0f; // 1 second timeout for requests
    private float decisionInterval = 1.0f; // 1 second between decisions

    private Rigidbody rb;
    private AIShooting shooting;
    public RLAgent opponentAgent;
    private HealthSystem healthSystem;
    private StreamWriter logFile;
    private Vector3 lastPosition;
    private float lastPositionTime;
    private float positionCheckInterval = 1f;
    private HashSet<Vector2Int> visitedPositions = new HashSet<Vector2Int>();
    private string instanceId;

    private float lastDecisionTime = 0f;

    [Header("Game Settings")]
    [SerializeField] private float gameDuration = 120f; // 2 minutes in seconds
    private float gameStartTime;

    private float lastHealth;
    private float lastOpponentHealth;
    private Vector3 lastOpponentPosition;
    private bool isDead = false;

    // RL specific fields
    private NeuralNetworkModel model;
    private ReplayBuffer buffer;
    private AdamOptimizer optimizer;
    private float epsilon;
    private float epsilonDecay;
    private float epsilonMin;
    private float gamma;
    private int batchSize;
    private float[] lastState;
    private float lastReward;
    private AIShooting shootingComponent;

    [System.Serializable]
    private class RewardData
    {
        public string instance_id;
        public float reward;
        public float[] next_state;
        public bool done;
        public int player_id;
    }

    [System.Serializable]
    private class ActionRequest
    {
        public float[] observation;
        public string instance_id;
        public int player_id;
    }

    void Awake()
    {
        Log($"[RLAgent] Awake called for {gameObject.name}");
        rb = GetComponent<Rigidbody>();
        shooting = GetComponent<AIShooting>();
        healthSystem = GetComponent<HealthSystem>();
        if (healthSystem != null)
        {
            healthSystem.OnDeath += OnAgentDeath;
        }
        lastPosition = transform.position;
        lastPositionTime = Time.time;
    }

    void Start()
    {
        try
        {
            // Use absolute path for logs
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string logPath = Path.Combine(projectRoot, "training", "build", "logs");
            Directory.CreateDirectory(logPath);
            string logFilePath = Path.Combine(logPath, $"rl_agent_{gameObject.name}_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            logFile = new StreamWriter(logFilePath, true);
            instanceId = System.Environment.GetEnvironmentVariable("INSTANCE_ID") ?? "unknown";
            
            // Share log file with AIShooting component
            if (shooting != null)
            {
                shooting.SetLogFile(logFile);
                Log($"[{gameObject.name}] Shared log file with AIShooting component");
                
                // Set up shooting
                if (opponentAgent != null)
                {
                    shooting.SetTarget(opponentAgent.gameObject);
                    shooting.StartShooting();
                    Log($"[{gameObject.name}] Set up shooting against opponent: {opponentAgent.gameObject.name}");
                }
                else
                {
                    Log($"[{gameObject.name}] Warning: No opponent agent found for shooting setup");
                }
            }
            else
            {
                Log($"[{gameObject.name}] Warning: AIShooting component not found");
            }
            
            // Set player ID based on IsPlayerOne
            playerId = IsPlayerOne ? 1 : 2;
            Log($"[{gameObject.name}] Initializing RL Agent as Player {playerId}");

            // Initialize the model
            model = new NeuralNetworkModel();
            model.Initialize();
            Log($"[{gameObject.name}] Neural network model initialized");

            // Initialize the buffer
            buffer = new ReplayBuffer(10000);
            Log($"[{gameObject.name}] Replay buffer initialized with capacity 10000");

            // Initialize the optimizer
            optimizer = new AdamOptimizer(0.001f);
            Log($"[{gameObject.name}] Optimizer initialized");

            // Check server connection
            StartCoroutine(CheckServerConnection());

            // Start training loop
            StartCoroutine(TrainingLoop());
        }
        catch (Exception e)
        {
            Log($"Error in Start: {e.Message}\n{e.StackTrace}");
        }
    }

    private IEnumerator CheckServerConnection()
    {
        string healthUrl = serverUrl.Replace("/get_action", "/health");
        Log($"Checking server connection at {healthUrl} for Player {playerId}");
        
        using (var uwr = new UnityWebRequest(healthUrl, "GET"))
        {
            uwr.downloadHandler = new DownloadHandlerBuffer();
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var response = JsonUtility.FromJson<HealthResponse>(uwr.downloadHandler.text);
                    if (response.status == "healthy")
                    {
                        isServerConnected = true;
                        Log($"Server connection established for Player {playerId}");
                    }
                    else
                    {
                        Log($"Server reported unhealthy status: {response.status}");
                    }
                }
                catch (Exception e)
                {
                    Log($"Error parsing health response: {e.Message}");
                }
            }
            else
            {
                Log($"Failed to connect to server: {uwr.error}");
            }
        }
    }

    [System.Serializable]
    private class HealthResponse
    {
        public string status;
        public int port;
        public int buffer_size;
        public int total_steps;
        public bool gpu_enabled;
    }

    void Update()
    {
        if (logFile == null)
        {
            Log($"Log file is null for {gameObject.name}");
            return;
        }

        // Check if game time has elapsed
        if (Time.time - gameStartTime >= gameDuration)
        {
            Log("Game time elapsed - Quitting application");
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
            return;
        }

        // Calculate and send rewards
        CalculateAndSendRewards();
        
        float checkTime = Time.time;
        if (checkTime - lastPositionTime >= positionCheckInterval)
        {
            CheckExploration();
            lastPositionTime = checkTime;
        }
        
        if (Time.time - lastDecisionTime >= decisionInterval)
        {
            TakeAction();
            lastDecisionTime = Time.time;
        }
    }

    private void CalculateAndSendRewards()
    {
        if (!isServerConnected || isDead) return;

        float currentHealth = healthSystem != null ? healthSystem.GetHealthPercentage() : 1f;
        float currentOpponentHealth = opponentAgent != null && opponentAgent.healthSystem != null ? 
            opponentAgent.healthSystem.GetHealthPercentage() : 1f;
        Vector3 currentOpponentPosition = opponentAgent != null ? opponentAgent.transform.position : Vector3.zero;

        float reward = 0f;

        // Health change reward
        float healthChange = currentHealth - lastHealth;
        reward += healthChange * 10f; // Positive reward for health gain, negative for health loss

        // Opponent health change reward
        float opponentHealthChange = lastOpponentHealth - currentOpponentHealth;
        reward += opponentHealthChange * 15f; // Positive reward for damaging opponent

        // Distance to opponent reward
        float currentDistance = Vector3.Distance(transform.position, currentOpponentPosition);
        float lastDistance = Vector3.Distance(transform.position, lastOpponentPosition);
        float distanceChange = lastDistance - currentDistance;
        reward += distanceChange * 0.1f; // Small reward for getting closer to opponent

        // Update last values
        lastHealth = currentHealth;
        lastOpponentHealth = currentOpponentHealth;
        lastOpponentPosition = currentOpponentPosition;

        // Send reward to server
        StartCoroutine(SendRewardToServer(reward));
    }

    private IEnumerator SendRewardToServer(float reward)
    {
        string rewardUrl = serverUrl.Replace("/get_action", "/update_reward");
        Log($"Sending reward to server at {rewardUrl} for Player {playerId}");
        float startTime = Time.realtimeSinceStartup;
        
        var rewardData = new RewardData
        {
            instance_id = instanceId,
            reward = reward,
            next_state = GetCurrentState(),
            done = isDead,
            player_id = playerId
        };

        string jsonPayload = JsonUtility.ToJson(rewardData);
        Log($"Reward payload for Player {playerId}: {jsonPayload}");

        using (var uwr = new UnityWebRequest(rewardUrl, "POST"))
        {
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonPayload);
            uwr.uploadHandler = new UploadHandlerRaw(jsonToSend);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");

            Log($"Sending reward request to {rewardUrl}");
            yield return uwr.SendWebRequest();

            float endTime = Time.realtimeSinceStartup;
            float requestTime = (endTime - startTime) * 1000f; // Convert to milliseconds

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Log($"Failed to send reward to server: {uwr.error} (took {requestTime:F2}ms)");
                Log($"Request URL: {rewardUrl}");
                Log($"Request payload: {jsonPayload}");
                Log($"Response code: {uwr.responseCode}");
                Log($"Response: {uwr.downloadHandler.text}");
            }
            else
            {
                Log($"Successfully sent reward to server. Response: {uwr.downloadHandler.text} (took {requestTime:F2}ms)");
            }
        }
    }

    private void TakeAction()
    {
        if (!isServerConnected)
        {
            Log($"Server not connected for {gameObject.name}");
            return;
        }

        try
        {
            float[] obs = GetCurrentState();
            Log($"Current state: [{string.Join(", ", obs)}]");
            
            StartCoroutine(GetActionFromServer(obs));
        }
        catch (Exception e)
        {
            Log($"Error in TakeAction: {e.Message}\n{e.StackTrace}");
        }
    }

    private IEnumerator GetActionFromServer(float[] observation)
    {
        Log($"Getting action from server at {serverUrl} for Player {playerId}");
        float startTime = Time.realtimeSinceStartup;
        
        var requestData = new ActionRequest
        {
            observation = observation,
            instance_id = instanceId,
            player_id = playerId
        };
        string jsonPayload = JsonUtility.ToJson(requestData);
        Log($"Action request payload for Player {playerId}: {jsonPayload}");

        using (var uwr = new UnityWebRequest(serverUrl, "POST"))
        {
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonPayload);
            uwr.uploadHandler = new UploadHandlerRaw(jsonToSend);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");
            uwr.timeout = Mathf.RoundToInt(requestTimeout * 1000); // Set timeout in milliseconds

            Log($"Sending action request to {serverUrl}");
            yield return uwr.SendWebRequest();

            float endTime = Time.realtimeSinceStartup;
            float requestTime = (endTime - startTime) * 1000f; // Convert to milliseconds

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Log($"Failed to get action from server: {uwr.error} (took {requestTime:F2}ms)");
                Log($"Request URL: {serverUrl}");
                Log($"Request payload: {jsonPayload}");
                Log($"Response code: {uwr.responseCode}");
                Log($"Response: {uwr.downloadHandler.text}");
                // Fallback to random action if server fails
                lastAction[0] = UnityEngine.Random.Range(-1f, 1f);
                lastAction[1] = UnityEngine.Random.Range(-1f, 1f);
                lastAction[2] = UnityEngine.Random.value;
            }
            else
            {
                try
                {
                    var response = JsonUtility.FromJson<ActionResponse>(uwr.downloadHandler.text);
                    float[] action = response.action;
                    
                    if (action == null || action.Length < 2)
                    {
                        Log("Invalid action received from server");
                        yield break;
                    }

                    Log($"Action received from server: [{string.Join(", ", action)}] (took {requestTime:F2}ms)");
                    lastAction = action;
                    
                    // Movement actions are in [-1, 1] range from tanh
                    float moveX = Mathf.Clamp(action[0], -1f, 1f);
                    float moveZ = Mathf.Clamp(action[1], -1f, 1f);
                    
                    // Shooting action is in [0, 1] range from sigmoid
                    float shoot = Mathf.Clamp(action[2], 0f, 1f);
                    
                    Vector3 move = new Vector3(moveX, 0, moveZ).normalized * movementSpeed;
                    
                    if (rb != null)
                    {
                        rb.linearVelocity = move;
                        if (move != Vector3.zero)
                        {
                            transform.rotation = Quaternion.LookRotation(move);
                        }
                    }
                    
                    // Use threshold for shooting (0.5 means shoot when action > 0.5)
                    if (shoot > 0.5f && shooting != null)
                    {
                        shooting.Shoot();
                    }
                }
                catch (Exception e)
                {
                    Log($"Error processing server response: {e.Message}\n{e.StackTrace}");
                }
            }
        }
    }

    [System.Serializable]
    private class ActionResponse
    {
        public float[] action;
    }

    private float[] GetCurrentState()
    {
        float[] state = new float[6];
        state[0] = healthSystem != null ? healthSystem.GetHealthPercentage() : 1f;
        state[1] = transform.position.x / 15f;  // Normalize position to [-1, 1] range
        state[2] = transform.position.z / 10f;
        if (opponentAgent != null)
        {
            state[3] = opponentAgent.healthSystem != null ? opponentAgent.healthSystem.GetHealthPercentage() : 1f;
            state[4] = opponentAgent.transform.position.x / 15f;
            state[5] = opponentAgent.transform.position.z / 10f;
        }
        else
        {
            state[3] = 1f;
            state[4] = 0f;
            state[5] = 0f;
        }
        return state;
    }

    private void CheckExploration()
    {
        // Check for stagnation
        if (Vector3.Distance(transform.position, lastPosition) < 0.1f)
        {
            // Apply stagnation penalty
            StartCoroutine(SendRewardToServer(-stagnationPenalty));
            Log($"Applied stagnation penalty: {-stagnationPenalty}");
        }

        // Check for exploration
        Vector2Int currentPos = new Vector2Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.z)
        );
        
        if (!visitedPositions.Contains(currentPos))
        {
            // Apply exploration reward
            StartCoroutine(SendRewardToServer(explorationReward));
            visitedPositions.Add(currentPos);
            Log($"Applied exploration reward: {explorationReward} at position {currentPos}");
        }

        // Update last position for next check
        lastPosition = transform.position;
    }

    public void OnHit(float damage)
    {
        Log($"Agent hit for {damage}");
    }

    public void OnHitOpponent()
    {
        Log($"Agent hit opponent");
    }

    private void OnAgentDeath()
    {
        Log($"Agent died");
        isDead = true;
        // Send final reward
        StartCoroutine(SendRewardToServer(-10f)); // Penalty for dying
    }

    private bool IsValidPosition(Vector3 newPosition)
    {
        float mapWidth = 15f;
        float mapLength = 10f;
        bool isWithinBounds =
            newPosition.x > -mapWidth &&
            newPosition.x < mapWidth &&
            newPosition.z > -mapLength &&
            newPosition.z < mapLength;
        if (!isWithinBounds)
        {
            Log($"Position out of bounds: {newPosition}. Bounds: X[{-mapWidth}, {mapWidth}], Z[{-mapLength}, {mapLength}]");
            return false;
        }
        float radius = 0.5f;
        bool hasCollision = Physics.CheckSphere(newPosition, radius, LayerMask.GetMask("Default"));
        if (hasCollision)
        {
            Log($"Collision detected at position: {newPosition} with radius: {radius}");
        }
        return !hasCollision;
    }

    void OnDestroy()
    {
        logFile?.Close();
        logFile = null;
        isServerConnected = false;
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
                Debug.LogError($"[RLAgent] Failed to write to log: {e.Message}");
            }
        }
    }

    private IEnumerator TrainingLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(decisionInterval);
            if (!isDead)
            {
                TakeAction();
            }
        }
    }

    private float[] GetState()
    {
        return GetCurrentState();
    }
} 