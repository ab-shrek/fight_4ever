using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class SceneBuilder : MonoBehaviour
{
    // Health and score system
    private Dictionary<GameObject, float> playerHealth = new Dictionary<GameObject, float>();
    private Dictionary<GameObject, int> playerScore = new Dictionary<GameObject, int>();
    private Dictionary<GameObject, TextMeshProUGUI> playerHealthUI = new Dictionary<GameObject, TextMeshProUGUI>();
    private Dictionary<GameObject, TextMeshProUGUI> playerScoreUI = new Dictionary<GameObject, TextMeshProUGUI>();
    private Dictionary<GameObject, int> playerWins = new Dictionary<GameObject, int>();
    private Dictionary<GameObject, TextMeshProUGUI> playerWinsUI = new Dictionary<GameObject, TextMeshProUGUI>();
    private float maxHealth = 100f;
    private bool isGameOver = false;

    void Start()
    {
        // Main ground
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.transform.position = new Vector3(0, 0, 0);
        ground.transform.localScale = new Vector3(30, 1, 20);
        ground.name = "Ground";
        // Load ground material from Resources
        Material groundMat = Resources.Load<Material>("GroundMat");
        ground.GetComponent<Renderer>().material = groundMat;

        // DrawGridLines(30, 20, 1f);
        CreatePlayerZones();
        SetupLighting();
        SetupCamera();
        SetupUI();

        // Create enclosing walls
        CreateEnclosingWalls();

        // Check MAP_TYPE environment variable for cover creation
        string mapTypeEnv = System.Environment.GetEnvironmentVariable("MAP_TYPE");
        if (string.IsNullOrEmpty(mapTypeEnv) || mapTypeEnv.ToLower() == "default")
        {
            Debug.Log("[SceneBuilder] MAP_TYPE is 'default' or not set: No covers will be created.");
        }
        else
        {
            Debug.Log($"[SceneBuilder] MAP_TYPE is '{mapTypeEnv}': Creating covers.");
            // Center cover
            CreateCover(new Vector3(0, 2, 0), new Vector3(1f, 4, 6), "CenterCover");
            // Left side cover
            CreateCover(new Vector3(-8, 2, 4), new Vector3(1f, 4, 4), "LeftCover1");
            CreateCover(new Vector3(-8, 2, -4), new Vector3(1f, 4, 4), "LeftCover2");
            // Right side cover
            CreateCover(new Vector3(8, 2, 4), new Vector3(1f, 4, 4), "RightCover1");
            CreateCover(new Vector3(8, 2, -4), new Vector3(1f, 4, 4), "RightCover2");
        }

        // Players - spawning behind walls, facing towards center
        var player1 = CreatePlayer(new Vector3(-13, 1, 4), Quaternion.Euler(0, 0, 0), "Player_1", Color.blue);
        var player2 = CreatePlayer(new Vector3(13, 1, -4), Quaternion.Euler(0, 0, 0), "Player_2", Color.red);
        // Register health and score for each player
        playerHealth[player1] = maxHealth;
        playerHealth[player2] = maxHealth;
        playerScore[player1] = 0;
        playerScore[player2] = 0;
        playerWins[player1] = 0;  // Initialize wins for player 1
        playerWins[player2] = 0;  // Initialize wins for player 2
        // Improved UI placement: top left and top right
        CreatePlayerUI(player1, new Vector2(0f, 1f), TextAlignmentOptions.TopLeft); // Top left
        CreatePlayerUI(player2, new Vector2(1f, 1f), TextAlignmentOptions.TopRight); // Top right

        // Set up RLAgent opponent references
        var agent1 = player1.GetComponent<RLAgent>();
        var agent2 = player2.GetComponent<RLAgent>();
        if (agent1 != null && agent2 != null)
        {
            agent1.opponentAgent = agent2;
            agent2.opponentAgent = agent1;
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

    void CreatePlayerZones()
    {
        float width = 30f, length = 20f;
        float zoneWidth = 6f;
        CreateZone(new Vector3(-width/2 + zoneWidth/2, 0.51f, 0), new Vector3(zoneWidth, 1.02f, length), new Color(0.2f, 0.4f, 1f, 0.18f));
        CreateZone(new Vector3(width/2 - zoneWidth/2, 0.51f, 0), new Vector3(zoneWidth, 1.02f, length), new Color(1f, 0.4f, 0.2f, 0.18f));
    }

    void CreateZone(Vector3 pos, Vector3 scale, Color color)
    {
        var zone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        zone.transform.position = pos;
        zone.transform.localScale = scale;
        Material zoneMat = Resources.Load<Material>("ZoneMat");
        zone.GetComponent<Renderer>().material = zoneMat;
    }

    void SetupLighting()
    {
        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = Color.white;
        light.intensity = 1.2f;
        light.shadows = LightShadows.Soft;
        lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);
        RenderSettings.ambientLight = new Color(0.2f, 0.2f, 0.3f);
    }

    void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            var camGO = new GameObject("Main Camera");
            cam = camGO.AddComponent<Camera>();
            cam.tag = "MainCamera";
        }

        // Use orthographic for a board-game look and better screen fill
        cam.orthographic = true;
        cam.orthographicSize = 11; // Adjust this value until the arena fills the screen nicely
        cam.transform.position = new Vector3(0, 30, 0);
        cam.transform.rotation = Quaternion.Euler(90, 0, 0);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.12f, 0.12f);
    }

    void SetupUI()
    {
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<UnityEngine.Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Remove central score/time UI
        // var textGO = new GameObject("ScoreText");
        // textGO.transform.SetParent(canvasGO.transform);
        // var text = textGO.AddComponent<TMPro.TextMeshProUGUI>();
        // text.text = "Score: 0 | Time: 00:00";
        // text.fontSize = 48;
        // text.alignment = TMPro.TextAlignmentOptions.Center;
        // text.color = Color.white;
        // var rect = text.GetComponent<RectTransform>();
        // rect.anchorMin = new Vector2(0.5f, 1f);
        // rect.anchorMax = new Vector2(0.5f, 1f);
        // rect.pivot = new Vector2(0.5f, 1f);
        // rect.anchoredPosition = new Vector2(0, -40);
        // rect.sizeDelta = new Vector2(600, 100);
        // textGO.AddComponent<UITimer>();
    }

    void CreateEnclosingWalls()
    {
        // Wall dimensions
        float wallHeight = 5f;
        float wallThickness = 1f;
        float mapWidth = 30f;
        float mapLength = 20f;

        // Remove creation of new material
        // Create walls and store them for damage
        List<GameObject> walls = new List<GameObject>();

        // North wall
        var northWall = CreateWall(new Vector3(0, wallHeight/2, mapLength/2), 
                  new Vector3(mapWidth + wallThickness*2, wallHeight, wallThickness),
                  "NorthWall");
        walls.Add(northWall);
        // South wall
        var southWall = CreateWall(new Vector3(0, wallHeight/2, -mapLength/2), 
                  new Vector3(mapWidth + wallThickness*2, wallHeight, wallThickness),
                  "SouthWall");
        walls.Add(southWall);
        // East wall
        var eastWall = CreateWall(new Vector3(mapWidth/2, wallHeight/2, 0), 
                  new Vector3(wallThickness, wallHeight, mapLength),
                  "EastWall");
        walls.Add(eastWall);
        // West wall
        var westWall = CreateWall(new Vector3(-mapWidth/2, wallHeight/2, 0), 
                  new Vector3(wallThickness, wallHeight, mapLength),
                  "WestWall");
        walls.Add(westWall);
    }

    GameObject CreateWall(Vector3 position, Vector3 scale, string name)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.name = name;
        Material wallMat = Resources.Load<Material>("WallMat");
        wall.GetComponent<Renderer>().material = wallMat;

        // Add Rigidbody and make it kinematic (static)
        Rigidbody rb = wall.AddComponent<Rigidbody>();
        rb.isKinematic = true; // Make it static
        rb.useGravity = false; // No gravity needed
        rb.constraints = RigidbodyConstraints.FreezeAll; // Prevent any movement

        // Ensure proper collider
        BoxCollider collider = wall.GetComponent<BoxCollider>();
        collider.isTrigger = false;
        collider.size = scale;
        collider.center = Vector3.zero;

        return wall;
    }

    void CreateCover(Vector3 position, Vector3 scale, string name)
    {
        GameObject cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cover.transform.position = position;
        cover.transform.localScale = scale;
        cover.name = name;
        Material coverMat = Resources.Load<Material>("CoverMat");
        cover.GetComponent<Renderer>().material = coverMat;

        // Add Rigidbody and make it kinematic (static)
        Rigidbody rb = cover.AddComponent<Rigidbody>();
        rb.isKinematic = true; // Make it static
        rb.useGravity = false; // No gravity needed
        rb.constraints = RigidbodyConstraints.FreezeAll; // Prevent any movement

        // Ensure the cover has a proper collider
        BoxCollider collider = cover.GetComponent<BoxCollider>();
        collider.isTrigger = false; // Make sure it's not a trigger
        collider.center = Vector3.zero; // Center the collider
    }

    GameObject CreatePlayer(Vector3 position, Quaternion rotation, string name, Color color)
    {
        GameObject player = AISetup.CreateAICharacter(position, rotation, name);
        Transform visual = player.transform.GetChild(0);
        if (visual != null)
        {
            visual.GetComponent<Renderer>().material.color = color;
        }

        // Set isPlayerOne based on the player's name
        var rlAgent = player.GetComponent<RLAgent>();
        if (rlAgent != null)
        {
            rlAgent.IsPlayerOne = (name == "Player_1");
        }

        return player;
    }

    void CreatePlayerUI(GameObject player, Vector2 anchor, TextAlignmentOptions alignment)
    {
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        float marginX = 120f;
        float marginY = -120f;

        // Health UI
        var healthGO = new GameObject(player.name + "_HealthUI");
        healthGO.transform.SetParent(canvas.transform);
        var healthText = healthGO.AddComponent<TextMeshProUGUI>();
        healthText.fontSize = 28;
        healthText.color = Color.cyan;
        healthText.alignment = alignment;
        var rect = healthGO.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        if (alignment == TextAlignmentOptions.TopRight) {
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-marginX-90f, marginY);
        } else {
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(marginX, marginY);
        }
        rect.sizeDelta = new Vector2(200, 40);
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
        // Reset health for all players
        foreach (var player in playerHealth.Keys.ToList())
        {
            playerHealth[player] = maxHealth;
            playerScore[player] = 0;
            UpdatePlayerUI(player);
        }
    }

    // Update OnPlayerHit to handle game over and restart
    public void OnPlayerHit(GameObject player, float damage)
    {
        if (playerHealth.ContainsKey(player) && !isGameOver)
        {
            playerHealth[player] -= damage;
            if (playerHealth[player] < 0) playerHealth[player] = 0;
            UpdatePlayerUI(player);

            Debug.Log($"{player.name} took {damage} damage. Health: {playerHealth[player]}");
            // Award point to the other player on every hit
            GameObject opponent = null;
            foreach (var p in playerHealth.Keys)
                if (p != player) opponent = p;
            if (opponent != null)
            {
                playerScore[opponent]++;
                UpdatePlayerUI(opponent);
            }

            if (playerHealth[player] <= 0)
            {
                isGameOver = true;
                Debug.Log($"{player.name} is defeated!");
                
                // Award win to the opponent
                if (opponent != null)
                {
                    playerWins[opponent]++;
                    UpdatePlayerUI(opponent);
                }

                // Restart the game after a short delay
                Invoke("RestartGame", 3f);
            }
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
