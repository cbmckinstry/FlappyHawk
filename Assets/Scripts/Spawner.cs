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
    public float groundSpawnHeight = -1.35f;

    [Header("Spawn Distribution")]
    [Range(0f, 1f)] public float obstacleSpawnChance = 0.8f;
    [Range(0f, 1f)] public float balloonWeight = 0.2f;
    [Range(0f, 1f)] public float siloWeight = 0.2f;
    [Range(0f, 1f)] public float turbineWeight = 0.2f;
    [Range(0f, 1f)] public float pipeWeight = 0.2f;
    [Range(0f, 1f)] public float cycloneBirdWeight = 0.2f;
    [Range(0f, 1f)] public float tornadoWeight = 0.2f;

    [Range(0f, 1f)] public float cornKernelWeight = 0.7f;
    [Range(0f, 1f)] public float helmetWeight = 0.3f;

    [SerializeField] private float defenseCarrierDelay = 3.0f; // tweak in Inspector
    private bool defenseCarrierSpawned = false;
    private float defenseTimer = 0f;    

    private float timer;
    private bool parentTornadoSpawned = false;
    private GameObject parentTornado;

    // Game Day Mode variables
    private int gameDayWavesCompleted = 0;
    private int enemiesInCurrentWave = 0;
    private bool ballSpawned = false;
    private float waveSpawnCooldown = 0f;
    private const float WAVE_SPAWN_DELAY = 0.5f;
    private int wavesSinceLastHelmet = 0;



// Goal posts (Game Day)
[SerializeField] private float goalPostSpawnRate = 10f; // seconds between posts on offense
[SerializeField] private int   maxGoalPostsPerDrive = 4; // cap per drive

private float goalPostTimer = 0f;
private int   goalPostsThisDrive = 0;
private bool  prevInDefenseRound = true;

// --- Kickstart window to ensure offense resumes after defense ---
[SerializeField] private float offenseKickstartSeconds = 1.5f; // can adjust in Inspector
private float offenseKickstartTimer = 0f;


// Optional: track instances if you want to clean them up on defense/reset
private readonly List<GameObject> activeGoalPosts = new();




    private void OnEnable()
    {
        GameManager.OnSpawnRateChanged += HandleSpawnRateChanged;

        spawnRate = GameManager.CurrentSpawnRate;
        timer = 0f;
    }

    private void OnDisable()
    {
        GameManager.OnSpawnRateChanged -= HandleSpawnRateChanged;
    }

    private void HandleSpawnRateChanged(float newRate)
    {
        spawnRate = Mathf.Max(0.2f, newRate);
        timer = 0f;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) return;

        if (GameManager.CurrentGameMode == GameManager.GameMode.GameDay)
            UpdateGameDaySpawning();
        else
            UpdateNormalSpawning();
    }

    private void UpdateNormalSpawning()
    {
        // Spawn tornado once in Hard mode
        if (!parentTornadoSpawned)
        {
            if (GameManager.CurrentDifficulty == GameManager.Difficulty.Hard)
            {
                SpawnParentTornado();
            }
            parentTornadoSpawned = true;
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
    if (gameDayMgr == null) return;

    // --- Edge-triggered transition handling (defense <-> offense) ---
    if (gameDayMgr.InDefenseRound != prevInDefenseRound)
{
    if (gameDayMgr.InDefenseRound)
    {
        // Entered DEFENSE
        defenseCarrierSpawned = false;
        defenseTimer = 0f;
        goalPostsThisDrive = 0;
        goalPostTimer = 0f;
    }
    else
    {
        // === Entered OFFENSE ===
        ballSpawned = false;                  // allow ball to spawn again
        timer = 0f;                           // reset wave timer
        waveSpawnCooldown = WAVE_SPAWN_DELAY; // small breathing room before first wave
        goalPostsThisDrive = 0;               // restart goal-post trickle
        goalPostTimer = 0f;

        offenseKickstartTimer = offenseKickstartSeconds; // <<< NEW
    }
    prevInDefenseRound = gameDayMgr.InDefenseRound;
}

    // --- DEFENSE: spawn exactly one ball-carrier bird, then stop spawning ---
    if (gameDayMgr.InDefenseRound)
{
    // Wait before spawning the single ball-carrier bird
    defenseTimer += Time.deltaTime;

    if (!defenseCarrierSpawned && defenseTimer >= defenseCarrierDelay)
    {
        SpawnBallCarrierAtScreenCenter();
        defenseCarrierSpawned = true;
    }

    // No other spawns during defense
    return;
}

    // --- OFFENSE: (normal drive) goal-post trickle + ball and bird waves ---

    // Continuous goal-post spawning while ON OFFENSE (before early returns)
    if (!gameDayMgr.IsSpawningPaused())
    {
        if (goalPostsThisDrive < maxGoalPostsPerDrive)
        {
            goalPostTimer += Time.deltaTime;
            if (goalPostTimer >= goalPostSpawnRate)
            {
                SpawnGoalPosts(gameDayMgr);
                goalPostsThisDrive++;
                goalPostTimer = 0f;
            }
        }
    }

    // Spawn the football once per drive
    if (!gameDayMgr.IsSpawningPaused() && !ballSpawned)
    {
        SpawnGameDayBall();
        ballSpawned = true;
        return;
    }

    // Respect any pause flags
    // Kickstart window: temporarily ignore lingering pause flags
    bool paused = gameDayMgr.IsSpawningPaused();
    bool carrierNow = gameDayMgr.IsBallCarrierSpawningThisFrame();

    if (!gameDayMgr.InDefenseRound)
    {
    if (offenseKickstartTimer > 0f)
    {
        offenseKickstartTimer -= Time.deltaTime;
        // During kickstart, ignore pause flags
    }
    else
    {
        if (paused || carrierNow) return;
    }
    }

    // Wave cadence
    if (waveSpawnCooldown > 0f)
    {
        waveSpawnCooldown -= Time.deltaTime;
        return;
    }

    timer += Time.deltaTime;
    if (timer >= spawnRate)
    {
        timer = 0f;

        // Your existing “helmet every ~5 waves” logic
        if (wavesSinceLastHelmet >= 5 && Random.value < 0.5f)
        {
            SpawnGameDayHelmet();
            wavesSinceLastHelmet = 0;
        }
        else
        {
            // OFFENSE: spawn multi-bird wave
            SpawnGameDayWave(false);
            wavesSinceLastHelmet++;
        }
    }
}

    private void SpawnGameDayBall()
    {
        if (footballPrefab == null) return;
        Vector3 spawnPos = transform.position + Vector3.up * Random.Range(minHeight, maxHeight);
        Instantiate(footballPrefab, spawnPos, Quaternion.identity);
        waveSpawnCooldown = WAVE_SPAWN_DELAY;
    }

    private void SpawnGameDayWave(bool isDefenseRound)
    {
        if (cycloneBirdPrefab == null) return;

        int enemiesToSpawn = Random.Range(1, 6);
        List<Vector3> positions = GetFormationPositions(enemiesToSpawn, Random.Range(0, 5));

        foreach (var pos in positions)
        {
            Instantiate(cycloneBirdPrefab, pos, Quaternion.identity);
        }

        gameDayWavesCompleted++;
        FindObjectOfType<GameDayManager>()?.OnWaveCompleted();
        waveSpawnCooldown = WAVE_SPAWN_DELAY;
    }

    private List<Vector3> GetFormationPositions(int count, int formationType)
    {
        List<Vector3> positions = new();
        Vector3 basePos = transform.position;

        switch (formationType)
        {
            case 0:
                for (int i = 0; i < count; i++)
                    positions.Add(basePos + Vector3.up * ((i - count / 2f) * 1.5f));
                break;

            case 1:
                for (int i = 0; i < count; i++)
                {
                    float xOffset = (i - count / 2f) * 1.5f;
                    positions.Add(basePos + new Vector3(xOffset, Random.Range(-0.4f, 0.4f)));
                }
                break;

            case 2:
                for (int i = 0; i < count; i++)
                    positions.Add(basePos + new Vector3(Random.Range(-2f, 2f), Random.Range(-1.5f, 1.5f)));
                break;

            case 3:
                for (int i = 0; i < count; i++)
                {
                    float offset = i * 0.7f;
                    positions.Add(basePos + new Vector3(offset * 0.3f, offset * 0.6f));
                }
                break;

            default:
                for (int i = 0; i < count; i++)
                    positions.Add(basePos + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(minHeight, maxHeight)));
                break;
        }
        return positions;
    }

    private void SpawnGameDayHelmet()
    {
        if (helmetPrefab == null) return;

        Vector3 spawnPos = transform.position;
        spawnPos.y = Random.Range(minHeight, maxHeight);

        if (Camera.main != null)
        {
            Vector3 screenRight = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, 0, 0));
            spawnPos.x = screenRight.x - 1f;
        }

        Instantiate(helmetPrefab, spawnPos, Quaternion.identity);
        FindObjectOfType<GameDayManager>()?.OnWaveCompleted();
        waveSpawnCooldown = WAVE_SPAWN_DELAY;
    }

    private void SpawnGoalPosts(GameDayManager gdm)
{
    GameObject prefab = gdm.CurrentGameDayDifficulty == GameManager.GameDayDifficulty.Pro
        ? goalPostProPrefab
        : goalPostEasyPrefab;

    if (prefab == null) return;

    // Spawn near right edge at ground height
    Vector3 spawnPos = transform.position;
    spawnPos.y = groundSpawnHeight;

    if (Camera.main != null)
    {
        Vector3 screenRight = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, 0, 0));

        spawnPos.x = screenRight.x + 0.5f;
    }

    var posts = Instantiate(prefab, spawnPos, Quaternion.identity);
    activeGoalPosts.Add(posts);
}




    public void SpawnBallCarrierAtScreenCenter()
    {
        if (cycloneBirdPrefab == null || Camera.main == null) return;

        Vector3 screenRight = Camera.main.ScreenToWorldPoint(
            new Vector3(Camera.main.pixelWidth, Camera.main.pixelHeight / 2f, 0f));
        Vector3 pos = new(screenRight.x + 1f, screenRight.y, 0f);

        GameObject enemy = Instantiate(cycloneBirdPrefab, pos, Quaternion.identity);
        Destroy(enemy.GetComponent<CycloneBird>());
        enemy.AddComponent<BallCarrierBird>();
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
        GameObject prefab = SelectRandomObstacle();
        if (prefab == null) return;

        GameObject obj = Instantiate(prefab, transform.position, Quaternion.identity);

        if (prefab == siloPrefab || prefab == turbinePrefab)
            obj.transform.position = new Vector3(obj.transform.position.x, groundSpawnHeight, obj.transform.position.z);
        else
            obj.transform.position += Vector3.up * Random.Range(minHeight, maxHeight);

        GameManager.RegisterPipe();
    }

    private void SpawnCollectible()
    {
        GameObject prefab = SelectRandomCollectible();
        if (prefab == null) return;

        GameObject obj = Instantiate(prefab, transform.position, Quaternion.identity);
        obj.transform.position += Vector3.up * Random.Range(minHeight, maxHeight);
    }

    private GameObject SelectRandomObstacle()
    {
        float rand = Random.value;
        float cumulative = 0f;

        cumulative += pipeWeight;
        if (rand < cumulative && pipePrefab != null) return pipePrefab;

        cumulative += balloonWeight;
        if (rand < cumulative && balloonPrefab != null) return balloonPrefab;

        cumulative += siloWeight;
        if (rand < cumulative && siloPrefab != null) return siloPrefab;

        cumulative += turbineWeight;
        if (rand < cumulative && turbinePrefab != null) return turbinePrefab;

        cumulative += cycloneBirdWeight;
        if (rand < cumulative && cycloneBirdPrefab != null) return cycloneBirdPrefab;

        // Tornado only spawns in Hard
        if (GameManager.CurrentDifficulty == GameManager.Difficulty.Hard)
        {
            cumulative += tornadoWeight;
            if (rand < cumulative && tornadoPrefab != null)
                return tornadoPrefab;
        }

        return pipePrefab;
    }

    private GameObject SelectRandomCollectible()
    {
        float rand = Random.value;
        if (rand < cornKernelWeight && cornKernelPrefab != null)
            return cornKernelPrefab;
        if (helmetPrefab != null)
            return helmetPrefab;
        return cornKernelPrefab;
    }

    private void SpawnParentTornado()
    {
        if (tornadoPrefab == null || parentTornadoSpawned) return;
        parentTornado = Instantiate(tornadoPrefab, Vector3.zero, Quaternion.identity);
        parentTornadoSpawned = true;
    }

    public void ResetSpawner()
{
    parentTornadoSpawned = false;
    timer = 0f;
    gameDayWavesCompleted = 0;
    enemiesInCurrentWave = 0;
    ballSpawned = false;
    waveSpawnCooldown = 0f;
    wavesSinceLastHelmet = 0;

    // Goal post timers/counters
    goalPostTimer = 0f;
    goalPostsThisDrive = 0;
    prevInDefenseRound = true;
    
    var gdm = FindObjectOfType<GameDayManager>();
    prevInDefenseRound = gdm != null ? gdm.InDefenseRound : false;

    defenseCarrierSpawned = false;
    defenseTimer = 0f;

    offenseKickstartTimer = 0f; // also reset the kickstart
    activeGoalPosts.Clear();


    activeGoalPosts.Clear();
}


    public void ResetGameDayBall() => ballSpawned = false;

    public int GetGameDayWavesCompleted() => gameDayWavesCompleted;
    public int GetEnemiesInCurrentWave() => enemiesInCurrentWave;
}
