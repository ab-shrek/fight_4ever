using UnityEngine;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class RLTraining : MonoBehaviour
{
    [SerializeField] private bool useGPUServer = false;
    [SerializeField] private string serverIP = "localhost";
    [SerializeField] private int serverPort = 5000;
    
    private TcpClient client;
    private NetworkStream stream;
    private bool isConnected = false;
    private Dictionary<string, object> observationSpace;
    private Dictionary<string, object> actionSpace;

    void Start()
    {
        if (useGPUServer)
        {
            ConnectToGPUServer();
        }
        else
        {
            // Initialize local training
            InitializeLocalTraining();
        }
    }

    void InitializeLocalTraining()
    {
        // Define observation space (example)
        observationSpace = new Dictionary<string, object>
        {
            { "player_position", new float[3] },
            { "opponent_position", new float[3] },
            { "player_health", 0f },
            { "opponent_health", 0f }
        };

        // Define action space (example)
        actionSpace = new Dictionary<string, object>
        {
            { "movement", new float[2] },  // x, z movement
            { "attack", false }            // attack action
        };
    }

    async void ConnectToGPUServer()
    {
        try
        {
            client = new TcpClient();
            await client.ConnectAsync(serverIP, serverPort);
            stream = client.GetStream();
            isConnected = true;
            Debug.Log("Connected to GPU server");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to connect to GPU server: {e.Message}");
        }
    }

    public async Task<Dictionary<string, object>> GetAction(Dictionary<string, object> observation)
    {
        if (useGPUServer)
        {
            return await GetActionFromServer(observation);
        }
        else
        {
            return GetLocalAction(observation);
        }
    }

    private Dictionary<string, object> GetLocalAction(Dictionary<string, object> observation)
    {
        // Implement basic local action selection (random for testing)
        return new Dictionary<string, object>
        {
            { "movement", new float[] { UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f) } },
            { "attack", UnityEngine.Random.value > 0.5f }
        };
    }

    private async Task<Dictionary<string, object>> GetActionFromServer(Dictionary<string, object> observation)
    {
        if (!isConnected) return GetLocalAction(observation);

        try
        {
            // Serialize observation
            string jsonObservation = JsonConvert.SerializeObject(observation);
            byte[] data = Encoding.UTF8.GetBytes(jsonObservation + "\n");
            
            // Send observation to server
            await stream.WriteAsync(data, 0, data.Length);
            
            // Read response
            byte[] buffer = new byte[4096];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            
            // Parse and return action
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(response);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error communicating with GPU server: {e.Message}");
            return GetLocalAction(observation);
        }
    }

    void OnDestroy()
    {
        if (isConnected)
        {
            stream?.Close();
            client?.Close();
        }
    }
} 