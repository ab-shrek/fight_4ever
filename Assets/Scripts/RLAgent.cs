using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

public class RLAgent : Agent
{
    [SerializeField] private bool useRemoteTraining = true;
    [SerializeField] private string serverIP = "localhost";
    [SerializeField] private int serverPort = 5000;  // Default port
    [SerializeField] private bool isPlayerOne = true;  // New field to identify which player this is
    [SerializeField] private float movementSpeed = 7f;
    [SerializeField] private float explorationReward = 0.01f;
    [SerializeField] private float stagnationPenalty = 0.005f;
    public bool IsPlayerOne 
    { 
        get => isPlayerOne;
        set 
        { 
            isPlayerOne = value;
            // Update port when isPlayerOne changes
            serverPort = isPlayerOne ? 5000 : 5001;
        }
    }
    
    private TcpClient client;
    private NetworkStream stream;
    private bool isConnected = false;
    private bool isConnecting = false;
    private float lastConnectionAttempt = 0f;
    private float connectionRetryInterval = 5f; // Retry connection every 5 seconds
    private Rigidbody rb;
    private AIShooting shooting;
    public RLAgent opponentAgent;
    private HealthSystem healthSystem;
    private StreamWriter logFile;
    private float lastRequestTime = 0f;
    private float requestInterval = 0.1f; // 100ms between requests
    private Vector3 lastPosition;
    private float lastPositionTime;
    private float positionCheckInterval = 1f; // Check position every second
    private HashSet<Vector2Int> visitedPositions = new HashSet<Vector2Int>();
    private double lastServerRequestTimestamp = 0;
    private string instanceId;

    protected override void Awake()
    {
        base.Awake();
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
            // Test write first
            // string testPath = Path.Combine("/Users/mario/fight_4ever", "training", "build", "logs", "test.txt");
            // File.WriteAllText(testPath, "test");
            
            // Initialize logging
            string logPath = Path.Combine("/Users/mario/fight_4ever", "training", "build", "logs");
            
            // Create new log file with player name
            string logFile = Path.Combine(logPath, $"agent_{gameObject.name}_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            this.logFile = new StreamWriter(logFile, true);
            Log($"Log file created at: {logFile}");

            // Initialize components
            rb = GetComponent<Rigidbody>();
            shooting = GetComponent<AIShooting>();
            healthSystem = GetComponent<HealthSystem>();
            Log("Components initialized");

            if (useRemoteTraining)
            {
                Log($"useRemoteTraining is true, isPlayerOne: {isPlayerOne}");
                int port = isPlayerOne ? 5000 : 5001;
                Log($"Connecting to training server on port {port}");
                _ = ConnectToTrainingServer(port);
            }
            else
            {
                Log("Not using remote training");
            }
            
            // Request initial decision
            Log("Requesting initial decision");
            RequestDecision();
            Log("Start method completed");

            instanceId = System.Environment.GetEnvironmentVariable("INSTANCE_ID") ?? gameObject.name;
        }
        catch (Exception e)
        {
            Log($"Error in Start: {e.Message}\n{e.StackTrace}");
        }
    }

    private void Update()
    {
        if (useRemoteTraining)
        {
            float currentTime = Time.time;
            
            // Check connection status and attempt reconnection if needed
            if (!isConnected && !isConnecting && currentTime - lastConnectionAttempt >= connectionRetryInterval)
            {
                Log("Connection lost, attempting to reconnect");
                int port = isPlayerOne ? 5000 : 5001;
                _ = ConnectToTrainingServer(port);
            }

            if (isConnected && currentTime - lastRequestTime >= requestInterval)
            {
                Log($"Update calling SendObservationsAndGetActions - Time: {currentTime}, Frame: {Time.frameCount}, Client Connected: {client?.Connected}");
                SendObservationsAndGetActions();
                Log($"Update calling RequestDecision - Time: {currentTime}, Frame: {Time.frameCount}, Client Connected: {client?.Connected}");
                RequestDecision();
                lastRequestTime = currentTime;
            }
        }

        // Check for exploration rewards
        float checkTime = Time.time;
        if (checkTime - lastPositionTime >= positionCheckInterval)
        {
            CheckExploration();
            lastPositionTime = checkTime;
        }
    }

    private async Task ConnectToTrainingServer(int port)
    {
        if (isConnecting)
        {
            Log("Already attempting to connect, skipping connection attempt");
            return;
        }

        float currentTime = Time.time;
        if (currentTime - lastConnectionAttempt < connectionRetryInterval)
        {
            Log("Too soon to retry connection");
            return;
        }

        isConnecting = true;
        lastConnectionAttempt = currentTime;

        try
        {
            Log($"Connecting to training server on port {port}...");
            client = new TcpClient();
            await client.ConnectAsync(serverIP, port);
            stream = client.GetStream();
            isConnected = true;
            Log($"Connected to training server on port {port}");
            
            // Add a small delay to ensure server is ready
            await Task.Delay(100);
            
            // Send initial observation immediately after connection
            SendObservationsAndGetActions();
        }
        catch (Exception e)
        {
            Log($"Failed to connect to training server: {e.Message}");
            isConnected = false;
        }
        finally
        {
            isConnecting = false;
        }
    }

    public override void Initialize()
    {
        // Initialize components
        rb = GetComponent<Rigidbody>();
        shooting = GetComponent<AIShooting>();
        healthSystem = GetComponent<HealthSystem>();
        
        // Configure behavior parameters
        var behaviorParameters = GetComponent<Unity.MLAgents.Policies.BehaviorParameters>();
        if (behaviorParameters != null)
        {
            // Set vector observation space size to 6 (1 health + 2 position + 1 opponent health + 2 opponent position)
            behaviorParameters.BrainParameters.VectorObservationSize = 6;
            // Set continuous action space size to 3 (2 for movement, 1 for shooting)
            behaviorParameters.BrainParameters.ActionSpec = Unity.MLAgents.Actuators.ActionSpec.MakeContinuous(3);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Observe own health (normalized)
        sensor.AddObservation(healthSystem != null ? healthSystem.GetHealthPercentage() : 1f);
        // Observe own position
        sensor.AddObservation(transform.position.x / 15f); // Normalize to map size
        sensor.AddObservation(transform.position.z / 10f);
        // Observe opponent's health and position
        if (opponentAgent != null)
        {
            sensor.AddObservation(opponentAgent.healthSystem != null ? opponentAgent.healthSystem.GetHealthPercentage() : 1f);
            sensor.AddObservation(opponentAgent.transform.position.x / 15f);
            sensor.AddObservation(opponentAgent.transform.position.z / 10f);
        }
        else
        {
            // If no opponent, add default values
            sensor.AddObservation(1f); // Default opponent health
            sensor.AddObservation(0f); // Default opponent x position
            sensor.AddObservation(0f); // Default opponent z position
        }
        // Do NOT add instanceId here (must be float)
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        Log($"OnActionReceived - Time: {Time.time}, Frame: {Time.frameCount}, Client Connected: {client?.Connected}");
        
        if (useRemoteTraining)
        {
            Log($"useRemoteTraining is true, isConnected: {isConnected}, Client Connected: {client?.Connected}");
            if (isConnected)
            {
                Log("Connected to server, sending observations...");
                SendObservationsAndGetActions();
            }
            else
            {
                Log("Not connected to training server, using default actions");
                ApplyDefaultAction();
            }
        }
        else
        {
            Log("Using local ML-Agents actions");
            float moveX = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
            float moveZ = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
            float shoot = actions.ContinuousActions.Length > 2 ? actions.ContinuousActions[2] : 0f;

            Vector3 move = new Vector3(moveX, 0, moveZ) * movementSpeed * Time.deltaTime;
            if (rb != null)
            {
                Vector3 newPosition = transform.position + move;
                if (IsValidPosition(newPosition))
                {
                    rb.MovePosition(newPosition);
                    if (move != Vector3.zero)
                    {
                        transform.rotation = Quaternion.LookRotation(move);
                    }
                }
                else
                {
                    AddReward(-0.01f);
                }
            }
            if (shoot > 0.5f && shooting != null)
            {
                shooting.Shoot();
            }
            AddReward(0.001f);
        }
    }

    private async void SendObservationsAndGetActions()
    {
        if (!isConnected) 
        {
            Log("SendObservationsAndGetActions: Not connected, returning");
            return;
        }

        try
        {
            Log($"SendObservationsAndGetActions: Starting - Client Connected: {client?.Connected}");
            // Create observation dictionary
            var observation = new Dictionary<string, object>
            {
                { "health", healthSystem != null ? healthSystem.GetHealthPercentage() : 1f },
                { "position", new float[] { transform.position.x / 15f, transform.position.z / 10f } },
                { "opponent_health", opponentAgent?.healthSystem != null ? opponentAgent.healthSystem.GetHealthPercentage() : 1f },
                { "opponent_position", opponentAgent != null ? new float[] { opponentAgent.transform.position.x / 15f, opponentAgent.transform.position.z / 10f } : new float[] { 0, 0 } },
                { "instance_id", instanceId }
            };

            Log($"Sending observation: {JsonConvert.SerializeObject(observation)}");

            // Check if connection is still valid
            if (!client?.Connected ?? true)
            {
                Log("Connection lost, attempting to reconnect");
                isConnected = false;
                int port = isPlayerOne ? 5000 : 5001;
                await ConnectToTrainingServer(port);
                if (!isConnected) return;
            }

            // Before sending observation
            lastServerRequestTimestamp = DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalMilliseconds;

            // Serialize and send observation
            string jsonObservation = JsonConvert.SerializeObject(observation);
            byte[] data = Encoding.UTF8.GetBytes(jsonObservation + "\n");
            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync();
            Log("Sent observation to server");

            // Read response with timeout
            byte[] buffer = new byte[4096];
            int bytesRead = 0;
            try
            {
                stream.ReadTimeout = 1000; // 1 second timeout
                bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            }
            catch (Exception e)
            {
                Log($"Timeout reading from server: {e.Message}");
                ApplyDefaultAction();
                return;
            }

            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
            Log($"Received response: {response}");
            
            if (string.IsNullOrEmpty(response))
            {
                Log("Received empty response from server");
                ApplyDefaultAction();
                return;
            }

            var action = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);
            if (action == null)
            {
                Log($"Failed to parse action from response: {response}");
                ApplyDefaultAction();
                return;
            }

            // Apply actions
            float[] movement = ((Newtonsoft.Json.Linq.JArray)action["movement"]).ToObject<float[]>();
            bool shoot = (bool)action["attack"];

            Vector3 move = new Vector3(movement[0], 0, movement[1]) * 5f * Time.deltaTime;
            Log($"Applying movement: {move}");
            
            if (rb != null)
            {
                rb.MovePosition(transform.position + move);
                if (move != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(move);
                }
            }

            if (shoot && shooting != null)
            {
                Log("Shooting!");
                shooting.Shoot();
            }

            // After receiving response
            double now = DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalMilliseconds;
            double lagMs = now - lastServerRequestTimestamp;
            Log($"Server response lag: {lagMs} ms | Instance: {instanceId}");
        }
        catch (Exception e)
        {
            Log($"Error communicating with training server: {e.Message}");
            Log(e.ToString());
            isConnected = false;
            ApplyDefaultAction();
        }
    }

    private void ApplyDefaultAction()
    {
        if (rb != null)
        {
            // Generate random movement direction with increased magnitude
            float randomX = UnityEngine.Random.Range(-1f, 1f);
            float randomZ = UnityEngine.Random.Range(-1f, 1f);
            Vector3 move = new Vector3(randomX, 0, randomZ) * 10f * Time.deltaTime; // Increased movement speed
            
            Vector3 newPosition = transform.position + move;
            // Check if new position would be valid
            if (IsValidPosition(newPosition))
            {
                rb.MovePosition(newPosition);
                Log($"Applied random movement: {move}, New position: {newPosition}");
                if (move != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(move);
                }
            }
            else
            {
                // Try a different random direction if blocked
                randomX = UnityEngine.Random.Range(-1f, 1f);
                randomZ = UnityEngine.Random.Range(-1f, 1f);
                move = new Vector3(randomX, 0, randomZ) * 10f * Time.deltaTime;
                newPosition = transform.position + move;
                
                if (IsValidPosition(newPosition))
                {
                    rb.MovePosition(newPosition);
                    Log($"Applied alternative random movement: {move}, New position: {newPosition}");
                    if (move != Vector3.zero)
                    {
                        transform.rotation = Quaternion.LookRotation(move);
                    }
                }
                else
                {
                    // Small penalty for being stuck
                    AddReward(-0.01f);
                    Log($"WARNING: Blocked movement in both directions. Current position: {transform.position}");
                }
            }

            // Random shooting (50% chance)
            if (shooting != null && UnityEngine.Random.value > 0.5f)
            {
                Log("Random shooting!");
                shooting.Shoot();
            }
        }
        else
        {
            Log("Rigidbody is null!");
        }
    }

    public void OnHit(float damage)
    {
        AddReward(-0.2f); // Negative reward for being hit
    }

    public void OnHitOpponent()
    {
        AddReward(0.5f); // Reward for hitting opponent
    }

    private void OnAgentDeath()
    {
        SetReward(-1.0f);
        EndEpisode();
        if (opponentAgent != null)
        {
            opponentAgent.SetReward(1.0f);
            opponentAgent.EndEpisode();
        }
    }

    void OnDestroy()
    {
        if (isConnected)
        {
            stream?.Close();
            client?.Close();
        }
        logFile?.Close();
    }

    private void Log(string message)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string logMessage = $"[{timestamp}] [{gameObject.name}] {message}";
        logFile.WriteLine(logMessage);
        logFile.Flush();
    }

    // Add this new method to check if a position is valid
    private bool IsValidPosition(Vector3 newPosition)
    {
        // Get the bounds of the arena
        float mapWidth = 15f;  // Half of 30
        float mapLength = 10f; // Half of 20
        
        // Check if the new position is within bounds
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
        
        // Check for collisions with walls using Physics.CheckSphere
        float radius = 0.5f; // Adjust this value based on your agent's size
        bool hasCollision = Physics.CheckSphere(newPosition, radius, LayerMask.GetMask("Default"));
        if (hasCollision)
        {
            Log($"Collision detected at position: {newPosition} with radius: {radius}");
        }
        return !hasCollision;
    }

    private void CheckExploration()
    {
        // Convert current position to grid coordinates (rounded to nearest meter)
        Vector2Int currentGridPos = new Vector2Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.z)
        );

        // If this is a new position, give exploration reward
        if (visitedPositions.Add(currentGridPos))
        {
            AddReward(explorationReward);
            Log($"Exploration reward: {explorationReward} at position {currentGridPos}");
        }

        // Check if agent is stuck in same area
        float distanceMoved = Vector3.Distance(transform.position, lastPosition);
        if (distanceMoved < 0.5f)
        {
            AddReward(-stagnationPenalty);
            Log($"Stagnation penalty: {-stagnationPenalty} - Distance moved: {distanceMoved}");
        }

        lastPosition = transform.position;
    }

    public override void OnEpisodeBegin()
    {
        // Reset visited positions when episode starts
        visitedPositions.Clear();
        lastPosition = transform.position;
        lastPositionTime = Time.time;
    }
} 