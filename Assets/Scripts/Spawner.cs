// Spawner.cs
using UnityEngine;
using System.Collections.Generic;

public class Spawner : MonoBehaviour
{
    [Header("Obstacle Prefabs")]
    public GameObject pipePrefab;
    public GameObject balloonPrefab;
    public GameObject siloPrefab;
    public GameObject turbinePrefab;
    public GameObject cycloneBirdPrefab;
    public GameObject tornadoPrefab;

    [Header("Collectible Prefabs")]
    public GameObject cornKernelPrefab;
    public GameObject helmetPrefab;

    [Header("Game Day Mode Prefabs")]
    public GameObject footballPrefab;
    public GameObject goalPostEasyPrefab;
    public GameObject goalPostProPrefab;

    [Header("Spawn Settings")]
    public float spawnRate = 1.2f;
    public float minHeight = -1f;
    public float maxHeight = 2f;
    public float groundSpawnHeight = -1.35f; // Height where Silos and Turbines spawn (touching ground)

    [Header("Spawn Distribution")]
    [Range(0f, 1f)] public float obstacleSpawnChance = 0.8f; // 80% chance to spawn obstacle vs collectible
    [Range(0f, 1f)] public float balloonWeight = 0.2f;
    [Range(0f, 1f)] public float siloWeight = 0.2f;
    [Range(0f, 1f)] public float turbineWeight = 0.2f;
    [Range(0f, 1f)] public float pipeWeight = 0.2f;
    [Range(0f, 1f)] public float cycloneBirdWeight = 0.2f;
    [Range(0f, 1f)] public float tornadoWeight = 0.2f;

    [Range(0f, 1f)] public float cornKernelWeight = 0.7f;
    [Range(0f, 1f)] public float helmetWeight = 0.3f;

    private float timer;
    private bool parentTornadoSpawned = false;
    private GameObject parentTornado;

    // Game Day Mode variables
    private int gameDayWavesCompleted = 0;
    private int enemiesInCurrentWave = 0;
    private bool ballSpawned = false;
    private float waveSpawnCooldown = 0f;
    private const float WAVE_SPAWN_DELAY = 0.5f; // Delay between spawning enemies in a wave
    private int wavesSinceLastHelmet = 0; // Track waves for helmet spawning

    private void OnEnable()
    {
        GameManager.OnSpawnRateChanged += HandleSpawnRateChanged;

        // Grab the current rate from GameManager (covers the "missed initial event" case)
        if (GameManager.Instance != null && GameManager.Instance.CurrentSpawnRate > 0f)
            spawnRate = GameManager.Instance.CurrentSpawnRate;

        timer = 0f; // apply immediately
    }

    private void OnDisable()
    {
        GameManager.OnSpawnRateChanged -= HandleSpawnRateChanged;
    }

    private void HandleSpawnRateChanged(float newRate)
    {
        spawnRate = Mathf.Max(0.2f, newRate); // clamp so it never hits zero/negative
        timer = 0f;                           // make the change visible right away
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) return;

        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("[Spawner] GameManager not found!");
            return;
        }

        if (gm.CurrentGameMode == GameMode.GameDay)
        {
            UpdateGameDaySpawning();
        }
        else
        {
            UpdateNormalSpawning();
        }
    }

    private void UpdateNormalSpawning()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentGameMode == GameMode.GameDay)
            return;

        if (!parentTornadoSpawned && GameManager.Instance != null)
        {
            Difficulty currentDifficulty = GameManager.Instance.CurrentDifficulty;
            Debug.Log($"[Spawner] Checking difficulty: {currentDifficulty} (Hard={Difficulty.Hard})");
            
            if (currentDifficulty == Difficulty.Hard)
            {
                Debug.Log("[Spawner] Spawning parent tornado in Hard mode!");
                SpawnParentTornado();
                parentTornadoSpawned = true;
            }
            else
            {
                parentTornadoSpawned = true;
            }
        }

        timer += Time.deltaTime;
        if (timer >= spawnRate)
        {
            timer = 0f;
            SpawnObstacleOrCollectible();
        }
    }

    private void UpdateGameDaySpawning()
    {
        GameDayManager gameDayMgr = FindObjectOfType<GameDayManager>();

        // Spawn football first if in offense round
        if (!gameDayMgr.IsInDefenseRound() && !ballSpawned)
        {
            Debug.Log("[Spawner] Game Day Mode: Spawning football");
            SpawnGameDayBall();
            ballSpawned = true;
            return;
        }

        // Don't spawn waves when ball carrier is being spawned
        if (gameDayMgr.IsBallCarrierSpawningThisFrame())
        {
            Debug.Log("[Spawner] Skipping wave spawn - ball carrier spawning instead");
            return;
        }

        if (waveSpawnCooldown > 0f)
        {
            waveSpawnCooldown -= Time.deltaTime;
            return;
        }

        // Don't spawn if currently paused during mode transition
        if (gameDayMgr.IsSpawningPaused())
            return;
        
        timer += Time.deltaTime;
        if (timer >= spawnRate)
        {
            timer = 0f;
            
            // Check if we should spawn a helmet instead of a wave (every 5 waves, 50% chance)
            if (wavesSinceLastHelmet >= 5 && Random.value < 0.5f)
            {
                Debug.Log("[Spawner] Game Day Mode: Spawning helmet instead of wave!");
                SpawnGameDayHelmet();
                wavesSinceLastHelmet = 0;
            }
            else
            {
                Debug.Log($"[Spawner] Game Day Mode: Spawning wave of enemies");
                SpawnGameDayWave(gameDayMgr.IsInDefenseRound());
                wavesSinceLastHelmet++;
            }
        }
    }

    private void SpawnGameDayBall()
    {
        if (footballPrefab != null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * Random.Range(minHeight, maxHeight);
            GameObject ball = Instantiate(footballPrefab, spawnPos, Quaternion.identity);
            Debug.Log($"[Spawner] Football spawned at {spawnPos}");
            
            // Set cooldown to prevent wave from spawning at same time
            waveSpawnCooldown = WAVE_SPAWN_DELAY;
        }
        else
        {
            Debug.LogWarning("[Spawner] Football prefab not assigned!");
        }
    }

    private void SpawnGameDayWave(bool isDefenseRound)
    {
        // Spawn a wave of 1-5 cyclone bird enemies in formation
        int enemiesToSpawn = Random.Range(1, 6); // 1-5 enemies
        
        enemiesInCurrentWave = enemiesToSpawn;

        // Choose formation type
        int formation = Random.Range(0, 5); // 0-4 for different formations

        List<Vector3> positions = GetFormationPositions(enemiesToSpawn, formation);

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            // Spawn regular cyclone bird using formation positions
            Vector3 spawnPos = positions[i];
            
            if (cycloneBirdPrefab != null)
            {
                GameObject enemy = Instantiate(cycloneBirdPrefab, transform.position, Quaternion.identity);
                enemy.transform.position = spawnPos;
            }
            else
            {
                Debug.LogWarning("[Spawner] Cyclone bird prefab not assigned!");
            }
        }

        gameDayWavesCompleted++;

        // Notify GameDayManager of wave completion for goal post spawning
        GameDayManager gameDayMgr = FindObjectOfType<GameDayManager>();
        if (gameDayMgr != null)
        {
            gameDayMgr.OnWaveCompleted();
        }

        // Set cooldown for next wave
        waveSpawnCooldown = WAVE_SPAWN_DELAY;
    }

    private List<Vector3> GetFormationPositions(int count, int formationType)
    {
        List<Vector3> positions = new List<Vector3>();
        Vector3 basePos = transform.position;

        switch (formationType)
        {
            case 0: // Vertical line - spread out vertically
                for (int i = 0; i < count; i++)
                {
                    float yOffset = (i - count / 2f) * 1.5f;
                    positions.Add(basePos + Vector3.up * yOffset);
                }
                break;

            case 1: // Horizontal spread - spread out horizontally
                for (int i = 0; i < count; i++)
                {
                    float xOffset = (i - count / 2f) * 1.5f;
                    float yOffset = Random.Range(-0.4f, 0.4f);
                    positions.Add(basePos + new Vector3(xOffset, yOffset, 0f));
                }
                break;

            case 2: // Loose cluster - more spread than tight
                for (int i = 0; i < count; i++)
                {
                    float randomX = Random.Range(-2f, 2f);
                    float randomY = Random.Range(-1.5f, 1.5f);
                    positions.Add(basePos + new Vector3(randomX, randomY, 0f));
                }
                break;

            case 3: // Diagonal formation - more spread
                for (int i = 0; i < count; i++)
                {
                    float offset = i * 0.7f;
                    positions.Add(basePos + new Vector3(offset * 0.3f, offset * 0.6f, 0f));
                }
                break;

            case 4: // Spread across screen
            default:
                for (int i = 0; i < count; i++)
                {
                    float randomY = Random.Range(minHeight, maxHeight);
                    float randomX = Random.Range(-0.5f, 0.5f);
                    positions.Add(basePos + new Vector3(randomX, randomY, 0f));
                }
                break;
        }

        return positions;
    }

    private void SpawnGameDayHelmet()
    {
        if (helmetPrefab == null)
        {
            Debug.LogWarning("[Spawner] Helmet prefab not assigned!");
            return;
        }

        // Spawn helmet at a random height on-screen
        Vector3 spawnPos = transform.position;
        spawnPos.y = Random.Range(minHeight, maxHeight);
        
        // Clamp X to be within visible screen bounds (right edge of screen)
        if (Camera.main != null)
        {
            Vector3 screenRightEdge = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, 0, 0));
            spawnPos.x = screenRightEdge.x - 1f; // Spawn at right edge
        }

        GameObject helmet = Instantiate(helmetPrefab, spawnPos, Quaternion.identity);
        Debug.Log($"[Spawner] Game Day helmet spawned at {spawnPos}");
        
        // Notify GameDayManager of wave completion for proper wave counting
        GameDayManager gameDayMgr = FindObjectOfType<GameDayManager>();
        if (gameDayMgr != null)
        {
            gameDayMgr.OnWaveCompleted();
        }
        
        // Set cooldown for next spawn
        waveSpawnCooldown = WAVE_SPAWN_DELAY;
    }

    public void SpawnBallCarrierAtScreenCenter()
    {
        if (cycloneBirdPrefab == null)
        {
            Debug.LogWarning("[Spawner] Cyclone bird prefab not assigned!");
            return;
        }

        if (Camera.main == null)
        {
            Debug.LogWarning("[Spawner] Camera not found!");
            return;
        }

        // Spawn at right edge of screen and middle height
        Vector3 screenRightEdge = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, Camera.main.pixelHeight / 2f, 0f));

        // Spawn at right edge, middle height
        Vector3 ballCarrierPos = new Vector3(
            screenRightEdge.x + 1f,                 // Slightly off right edge so it travels left across screen
            screenRightEdge.y,                      // Middle height
            0f
        );

        GameObject enemy = Instantiate(cycloneBirdPrefab, ballCarrierPos, Quaternion.identity);

        // Remove CycloneBird component and add BallCarrierBird
        CycloneBird cyclone = enemy.GetComponent<CycloneBird>();
        if (cyclone != null)
        {
            Destroy(cyclone);
        }

        BallCarrierBird ballCarrier = enemy.AddComponent<BallCarrierBird>();
        Debug.Log($"[Spawner] Ball carrier bird spawned at right edge: {ballCarrierPos}");
    }

    private void SpawnObstacleOrCollectible()
    {
        if (Random.value < obstacleSpawnChance)
            SpawnObstacle();
        else
            SpawnCollectible();
    }

    private void SpawnObstacle()
    {
        GameObject obstaclePrefab = SelectRandomObstacle();
        if (obstaclePrefab == null) return;

        GameObject obstacle = Instantiate(obstaclePrefab, transform.position, Quaternion.identity);
        
        // Silos and Turbines always spawn at ground level (y = groundSpawnHeight)
        // Other obstacles spawn at random heights
        if (obstaclePrefab == siloPrefab || obstaclePrefab == turbinePrefab)
        {
            obstacle.transform.position = new Vector3(obstacle.transform.position.x, groundSpawnHeight, obstacle.transform.position.z);
        }
        else
        {
            obstacle.transform.position += Vector3.up * Random.Range(minHeight, maxHeight);
        }
        
        GameManager.Instance?.RegisterPipe();
    }

    private void SpawnCollectible()
    {
        GameObject collectiblePrefab = SelectRandomCollectible();
        if (collectiblePrefab == null) return;

        GameObject collectible = Instantiate(collectiblePrefab, transform.position, Quaternion.identity);
        collectible.transform.position += Vector3.up * Random.Range(minHeight, maxHeight);
    }

    private GameObject SelectRandomObstacle()
    {
        float rand = Random.value;
        float cumulative = 0f;

        cumulative += pipeWeight;
        if (rand < cumulative && pipePrefab != null)
            return pipePrefab;

        cumulative += balloonWeight;
        if (rand < cumulative && balloonPrefab != null)
            return balloonPrefab;

        cumulative += siloWeight;
        if (rand < cumulative && siloPrefab != null)
            return siloPrefab;

        cumulative += turbineWeight;
        if (rand < cumulative && turbinePrefab != null)
            return turbinePrefab;

        cumulative += cycloneBirdWeight;
        if (rand < cumulative && cycloneBirdPrefab != null)
            return cycloneBirdPrefab;

        // Tornado only spawns in Hard mode
        if (GameManager.Instance != null && GameManager.Instance.CurrentDifficulty == Difficulty.Hard)
        {
            cumulative += tornadoWeight;
            if (rand < cumulative && tornadoPrefab != null)
                return tornadoPrefab;
        }

        return pipePrefab; // fallback
    }

    private GameObject SelectRandomCollectible()
    {
        float rand = Random.value;

        if (rand < cornKernelWeight && cornKernelPrefab != null)
            return cornKernelPrefab;

        if (helmetPrefab != null)
            return helmetPrefab;

        return cornKernelPrefab; // fallback
    }

    private void SpawnParentTornado()
    {
        if (tornadoPrefab == null)
        {
            Debug.LogWarning("Tornado prefab not assigned to Spawner!");
            return;
        }

        if (parentTornadoSpawned)
            return;

        // Instantiate the parent tornado at a fixed position
        // The Tornado script will handle positioning it at X=-9 and Y center
        parentTornado = Instantiate(tornadoPrefab, Vector3.zero, Quaternion.identity);
        parentTornadoSpawned = true;
    }

    public void ResetSpawner()
    {
        parentTornadoSpawned = false;
        timer = 0f;
        gameDayWavesCompleted = 0;
        enemiesInCurrentWave = 0;
        ballSpawned = false;  // Reset ball spawning for next offense round
        waveSpawnCooldown = 0f;
        wavesSinceLastHelmet = 0; // Reset helmet wave counter
    }

    public void ResetGameDayBall()
    {
        // Call this when transitioning to offense mode to allow ball respawn
        ballSpawned = false;
    }

    public int GetGameDayWavesCompleted()
    {
        return gameDayWavesCompleted;
    }

    public int GetEnemiesInCurrentWave()
    {
        return enemiesInCurrentWave;
    }
}
