using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using Unity.Barracuda;
using System;
using System.IO;
using System.Collections;
using UnityEngine.Networking;

public class SceneBuilder : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float mapWidth = 30f;
    [SerializeField] private float mapLength = 20f;
    [SerializeField] private float coverHeight = 2f;
    [SerializeField] private float coverWidth = 2f;
    [SerializeField] private float coverLength = 2f;
    [SerializeField] private int numCovers = 5;
    [SerializeField] private float marginX = 20f;
    [SerializeField] private float marginY = 20f;

    // Health and score system
    private Dictionary<GameObject, float> playerHealth = new Dictionary<GameObject, float>();
    private Dictionary<GameObject, int> playerScore = new Dictionary<GameObject, int>();
    private Dictionary<GameObject, TextMeshProUGUI> playerHealthUI = new Dictionary<GameObject, TextMeshProUGUI>();
    private Dictionary<GameObject, TextMeshProUGUI> playerScoreUI = new Dictionary<GameObject, TextMeshProUGUI>();
    private Dictionary<GameObject, int> playerWins = new Dictionary<GameObject, int>();
    private Dictionary<GameObject, TextMeshProUGUI> playerWinsUI = new Dictionary<GameObject, TextMeshProUGUI>();
    private bool isGameOver = false;
    private StreamWriter logFile;
    private string instanceId;

    void Awake()
    {
    }

    void Start()
    {
        try
        {
            // Set up bullet prefab first
            SetupBulletPrefab();

            // Use absolute path for logs
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string logPath = Path.Combine(projectRoot, "training", "build", "logs");
            Directory.CreateDirectory(logPath);
            string logFilePath = Path.Combine(logPath, $"scene_builder_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            logFile = new StreamWriter(logFilePath, true);
            instanceId = System.Environment.GetEnvironmentVariable("INSTANCE_ID") ?? "unknown";
            Log("SceneBuilder initialized");

            CreateGround();
            CreateWalls();
            CreateCovers();
            CreatePlayers();
            DrawGridLines(mapWidth, mapLength, 1f);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SceneBuilder] Error in Start: {e.Message}\n{e.StackTrace}");
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
                Debug.LogError($"[SceneBuilder] Failed to write to log: {e.Message}");
            }
        }
    }

    void DrawGridLines(float width, float length, float cellSize)
    {
        for (int x = -Mathf.FloorToInt(width/2); x <= Mathf.CeilToInt(width/2); x++)
        {
            CreateGridLine(new Vector3(x, 0.5f, -length/2), new Vector3(x, 0.5f, length/2));
        }
        for (int z = -Mathf.FloorToInt(length/2); z <= Mathf.CeilToInt(length/2); z++)
        {
            CreateGridLine(new Vector3(-width/2, 0.5f, z), new Vector3(width/2, 0.5f, z));
        }
    }

    void CreateGridLine(Vector3 start, Vector3 end)
    {
        var go = new GameObject("GridLine");
        
        // Create a cube for the line
        var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
        line.transform.SetParent(go.transform);
        
        // Calculate position and scale
        Vector3 midPoint = (start + end) / 2f;
        float length = Vector3.Distance(start, end);
        
        // Set position and scale
        line.transform.position = midPoint;
        line.transform.localScale = new Vector3(0.1f, 0.1f, length);
        
        // Rotate to align with direction
        Vector3 direction = end - start;
        if (direction.x != 0) // Horizontal line
        {
            line.transform.rotation = Quaternion.Euler(0, 90, 0);
        }
        
        // Set material
        Material gridMat = Resources.Load<Material>("GridMat");
        if (gridMat == null)
        {
            gridMat = new Material(Shader.Find("Standard"));
            gridMat.color = new Color(1f, 1f, 1f, 0.5f);
        }
        line.GetComponent<Renderer>().material = gridMat;
        
        // Set the layer to "Grid"
        go.layer = LayerMask.NameToLayer("Grid");
    }

    void CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.transform.localScale = new Vector3(mapWidth/10f, 1, mapLength/10f);
        ground.name = "Ground";
        Log("Ground created");
    }

    void CreateWalls()
    {
        float wallHeight = 5f;
        float wallThickness = 1f;

        // North wall
        CreateWall(new Vector3(0, wallHeight/2, mapLength/2), new Vector3(mapWidth, wallHeight, wallThickness), "NorthWall");
        // South wall
        CreateWall(new Vector3(0, wallHeight/2, -mapLength/2), new Vector3(mapWidth, wallHeight, wallThickness), "SouthWall");
        // East wall
        CreateWall(new Vector3(mapWidth/2, wallHeight/2, 0), new Vector3(wallThickness, wallHeight, mapLength), "EastWall");
        // West wall
        CreateWall(new Vector3(-mapWidth/2, wallHeight/2, 0), new Vector3(wallThickness, wallHeight, mapLength), "WestWall");
        Log("Walls created");
    }

    void CreateWall(Vector3 position, Vector3 size, string name)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.position = position;
        wall.transform.localScale = size;
        wall.name = name;
    }

    void CreateCovers()
    {
        for (int i = 0; i < numCovers; i++)
        {
            float x = UnityEngine.Random.Range(-mapWidth/2 + coverWidth, mapWidth/2 - coverWidth);
            float z = UnityEngine.Random.Range(-mapLength/2 + coverLength, mapLength/2 - coverLength);
            CreateCover(new Vector3(x, coverHeight/2, z));
        }
        Log($"Created {numCovers} covers");
    }

    void CreateCover(Vector3 position)
    {
        GameObject cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cover.transform.position = position;
        cover.transform.localScale = new Vector3(coverWidth, coverHeight, coverLength);
        cover.name = "Cover";
    }

    void CreatePlayers()
    {
        // Create canvas for UI
        GameObject canvas = new GameObject("Canvas");
        canvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Create players
        GameObject player1 = AISetup.CreateAICharacter(new Vector3(-5, 1, 0), Quaternion.identity, "Player1");
        GameObject player2 = AISetup.CreateAICharacter(new Vector3(5, 1, 0), Quaternion.identity, "Player2");

        // Set up RLAgent for player1
        RLAgent agent1 = player1.GetComponent<RLAgent>();
        if (agent1 != null)
        {
            agent1.IsPlayerOne = true;
        }

        // Set up RLAgent for player2
        RLAgent agent2 = player2.GetComponent<RLAgent>();
        if (agent2 != null)
        {
            agent2.IsPlayerOne = false;
        }

        // Pass log file to AIShooting components
        AIShooting shooting1 = player1.GetComponent<AIShooting>();
        if (shooting1 != null)
        {
            shooting1.SetLogFile(logFile);
            Log($"Set up logging for Player1's AIShooting component");
        }

        AIShooting shooting2 = player2.GetComponent<AIShooting>();
        if (shooting2 != null)
        {
            shooting2.SetLogFile(logFile);
            Log($"Set up logging for Player2's AIShooting component");
        }

        // Set up bullet prefab logging
        GameObject bulletPrefab = Resources.Load<GameObject>("Bullet");
        if (bulletPrefab != null)
        {
            Bullet bulletComponent = bulletPrefab.GetComponent<Bullet>();
            if (bulletComponent != null)
            {
                bulletComponent.SetLogFile(logFile);
                Log("Set up logging for Bullet prefab");
            }
        }

        // Initialize health and score
        playerHealth[player1] = maxHealth;
        playerHealth[player2] = maxHealth;
        playerScore[player1] = 0;
        playerScore[player2] = 0;
        playerWins[player1] = 0;
        playerWins[player2] = 0;

        // Create UI
        CreatePlayerUI(player1, new Vector2(0f, 1f), TextAlignmentOptions.TopLeft);
        CreatePlayerUI(player2, new Vector2(1f, 1f), TextAlignmentOptions.TopRight);

        // Set up RLAgent opponent references
        if (agent1 != null && agent2 != null)
        {
            agent1.opponentAgent = agent2;
            agent2.opponentAgent = agent1;
        }

        Log("Players created and initialized");
    }

    void CreatePlayerUI(GameObject player, Vector2 anchor, TextAlignmentOptions alignment)
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null) return;

        // Health UI
        var healthGO = new GameObject(player.name + "_HealthUI");
        healthGO.transform.SetParent(canvas.transform);
        var healthText = healthGO.AddComponent<TextMeshProUGUI>();
        healthText.fontSize = 28;
        healthText.color = Color.red;
        healthText.alignment = alignment;
        var rect1 = healthGO.GetComponent<RectTransform>();
        rect1.anchorMin = anchor;
        rect1.anchorMax = anchor;
        if (alignment == TextAlignmentOptions.TopRight) {
            rect1.pivot = new Vector2(1f, 1f);
            rect1.anchoredPosition = new Vector2(-marginX, marginY);
        } else {
            rect1.pivot = new Vector2(0f, 1f);
            rect1.anchoredPosition = new Vector2(marginX, marginY);
        }
        rect1.sizeDelta = new Vector2(200, 40);
        playerHealthUI[player] = healthText;

        // Score UI
        var scoreGO = new GameObject(player.name + "_ScoreUI");
        scoreGO.transform.SetParent(canvas.transform);
        var scoreText = scoreGO.AddComponent<TextMeshProUGUI>();
        scoreText.fontSize = 28;
        scoreText.color = Color.yellow;
        scoreText.alignment = alignment;
        var rect2 = scoreGO.GetComponent<RectTransform>();
        rect2.anchorMin = anchor;
        rect2.anchorMax = anchor;
        if (alignment == TextAlignmentOptions.TopRight) {
            rect2.pivot = new Vector2(1f, 1f);
            rect2.anchoredPosition = new Vector2(-marginX-90f, marginY - 40);
        } else {
            rect2.pivot = new Vector2(0f, 1f);
            rect2.anchoredPosition = new Vector2(marginX, marginY - 40);
        }
        rect2.sizeDelta = new Vector2(200, 40);
        playerScoreUI[player] = scoreText;

        // Wins UI
        var winsGO = new GameObject(player.name + "_WinsUI");
        winsGO.transform.SetParent(canvas.transform);
        var winsText = winsGO.AddComponent<TextMeshProUGUI>();
        winsText.fontSize = 28;
        winsText.color = Color.green;
        winsText.alignment = alignment;
        var rect3 = winsGO.GetComponent<RectTransform>();
        rect3.anchorMin = anchor;
        rect3.anchorMax = anchor;
        if (alignment == TextAlignmentOptions.TopRight) {
            rect3.pivot = new Vector2(1f, 1f);
            rect3.anchoredPosition = new Vector2(-marginX-90f, marginY - 80);
        } else {
            rect3.pivot = new Vector2(0f, 1f);
            rect3.anchoredPosition = new Vector2(marginX, marginY - 80);
        }
        rect3.sizeDelta = new Vector2(200, 40);
        playerWinsUI[player] = winsText;

        UpdatePlayerUI(player);
        Log($"Created UI for {player.name}");
    }

    void UpdatePlayerUI(GameObject player)
    {
        if (playerHealthUI.ContainsKey(player))
            playerHealthUI[player].text = $"Health: {playerHealth[player]}";
        if (playerScoreUI.ContainsKey(player))
            playerScoreUI[player].text = $"Score: {playerScore[player]}";
        if (playerWinsUI.ContainsKey(player))
            playerWinsUI[player].text = $"Wins: {playerWins[player]}";
    }

    void RestartGame()
    {
        isGameOver = false;
        foreach (var player in playerHealth.Keys.ToList())
        {
            playerHealth[player] = maxHealth;
            playerScore[player] = 0;
            UpdatePlayerUI(player);
        }
        Log("Game restarted");
    }

    public void OnPlayerHit(GameObject player, float damage, Vector3 hitPosition)
    {
        if (playerHealth.ContainsKey(player) && !isGameOver)
        {
            playerHealth[player] -= damage;
            if (playerHealth[player] < 0) playerHealth[player] = 0;
            UpdatePlayerUI(player);

            Log($"{player.name} took {damage} damage. Health: {playerHealth[player]}");
            
            // Award point to the other player on every hit
            GameObject opponent = null;
            foreach (var p in playerHealth.Keys)
                if (p != player) opponent = p;
            if (opponent != null)
            {
                playerScore[opponent]++;
                UpdatePlayerUI(opponent);
                Log($"{opponent.name} scored a point. New score: {playerScore[opponent]}");
            }

            if (playerHealth[player] <= 0)
            {
                isGameOver = true;
                Log($"{player.name} is defeated!");
                
                // Award win to the opponent
                if (opponent != null)
                {
                    playerWins[opponent]++;
                    UpdatePlayerUI(opponent);
                    Log($"{opponent.name} wins the match! Final score - Health: {playerHealth[opponent]}, Score: {playerScore[opponent]}, Wins: {playerWins[opponent]}");
                }

                // End the game instance after a short delay
                Invoke("QuitApplication", 3f);
            }

            // Send accuracy information to training server
            var accuracyData = new
            {
                player_id = player.name == "Player1" ? 1 : 2,
                hit = true,
                damage = damage,
                position = new { x = hitPosition.x, y = hitPosition.y, z = hitPosition.z }
            };

            StartCoroutine(SendAccuracyData(accuracyData));
        }
    }

    private IEnumerator SendAccuracyData(object accuracyData)
    {
        string jsonData = JsonUtility.ToJson(accuracyData);
        using (UnityWebRequest www = UnityWebRequest.PostWwwForm("http://localhost:5000/accuracy", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            yield return www.SendWebRequest();
            
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error sending accuracy data: {www.error}");
            }
        }
    }

    private void OnPlayerMiss(Vector3 missPosition)
    {
        // Send miss information to training server
        var accuracyData = new
        {
            player_id = 0,
            hit = false,
            damage = 0f,
            position = new { x = missPosition.x, y = missPosition.y, z = missPosition.z }
        };

        StartCoroutine(SendAccuracyData(accuracyData));
    }

    private void QuitApplication()
    {
        Log("Ending game instance...");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    void OnDestroy()
    {
        logFile?.Close();
        logFile = null;
    }

    private void SetupBulletPrefab()
    {
        try
        {
            // Create bullet prefab if it doesn't exist
            GameObject bulletPrefab = Resources.Load<GameObject>("Prefabs/Bullet");
            if (bulletPrefab == null)
            {
                // Create a new bullet GameObject
                GameObject bullet = new GameObject("Bullet");
                
                // Add components
                bullet.AddComponent<Bullet>();
                bullet.AddComponent<SphereCollider>();
                
                // Set up transform
                bullet.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                
                // Set up collider
                SphereCollider collider = bullet.GetComponent<SphereCollider>();
                collider.radius = 0.2f;
                collider.isTrigger = true;
                
                // Set up Bullet component
                Bullet bulletComponent = bullet.GetComponent<Bullet>();
                bulletComponent.speed = 20f;
                bulletComponent.damage = 10f;
                
                // Create Resources/Prefabs directory if it doesn't exist
                string prefabsPath = "Assets/Resources/Prefabs";
                if (!Directory.Exists(prefabsPath))
                {
                    Directory.CreateDirectory(prefabsPath);
                }
                
                // Save as prefab
                #if UNITY_EDITOR
                UnityEditor.PrefabUtility.SaveAsPrefabAsset(bullet, "Assets/Resources/Prefabs/Bullet.prefab");
                DestroyImmediate(bullet);
                #endif
                
                Log("Created Bullet prefab");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SceneBuilder] Error setting up bullet prefab: {e.Message}\n{e.StackTrace}");
        }
    }
}

// Timer script for UI
public class UITimer : MonoBehaviour
{
    TMPro.TextMeshProUGUI text;
    float timer = 0f;
    int score = 0;

    void Awake() => text = GetComponent<TMPro.TextMeshProUGUI>();

    void Update()
    {
        timer += Time.deltaTime;
        int minutes = Mathf.FloorToInt(timer / 60F);
        int seconds = Mathf.FloorToInt(timer % 60F);
        text.text = $"Score: {score} | Time: {minutes:00}:{seconds:00}";
    }
}
