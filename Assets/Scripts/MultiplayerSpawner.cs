using UnityEngine;
using System.Collections.Generic;

public class MultiplayerSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject cycloneBirdPrefab;
    public GameObject goalPostPrefab;
    public GameObject enemyBallCarrierPrefab;
    public GameObject footballPrefab;

    [Header("Spawn Settings")]
    public float spawnRate = 1.2f;
    public float minY = -1f;
    public float maxY = 2f;

    [Header("Defense Settings")]
    [Tooltip("Latest time (seconds) by which the carrier MUST have spawned in defense.")]
    public float defenseCarrierDelay = 3f;

    // Offense timers
    private float spawnTimer = 0f;
    private float goalPostTimer = 0f;
    private const float GOALPOST_RATE = 8f;

    // Defense state
    private bool defenseCarrierSpawned = false;
    private float defenseTimer = 0f;

    // NEW: continuous defense waves
    private float defenseWaveTimer = 0f;
    [SerializeField] private float defenseWaveIntervalMin = 0.45f;
    [SerializeField] private float defenseWaveIntervalMax = 0.8f;
    private float defenseWaveIntervalCurrent = 0.6f;

    private int defenseWavesSpawned = 0;
    private int defenseCarrierTargetWave = -1;   // which wave will carry the ball

    private MultiplayerManager mp => MultiplayerManager.Instance;

    private List<GameObject> spawnedObjects = new List<GameObject>();

    // ===========================================================
    // UPDATE
    // ===========================================================

    private void Update()
    {
        if (!PauseManager.GameIsActive || mp == null)
            return;

        if (mp.InDefenseRound)
            UpdateDefenseSpawning();
        else
            UpdateOffenseSpawning();
    }

    // ===========================================================
    // ====================== OFFENSE ============================
    // ===========================================================

    private void UpdateOffenseSpawning()
    {
        spawnTimer += Time.deltaTime;
        goalPostTimer += Time.deltaTime;

        if (spawnTimer >= spawnRate)
        {
            spawnTimer = 0f;
            SpawnRandomOffensePattern();
        }

        if (goalPostTimer >= GOALPOST_RATE)
        {
            goalPostTimer = 0f;
            SpawnGoalPost();
        }
    }

    private void SpawnRandomOffensePattern()
    {
        int type = Random.Range(0, 4);

        switch (type)
        {
            case 0: SpawnCluster(6, 12, 0.65f); break;
            case 1: SpawnDiagonal(6, 10, 0.45f); break;
            case 2: SpawnLine(6, 12, 0.5f); break;
            case 3: SpawnChaosSpray(8, 16); break;  // chaos pattern
        }
    }

    private void SpawnCluster(int minCount, int maxCount, float spread)
    {
        int count = Random.Range(minCount, maxCount);
        float midY = Random.Range(minY, maxY);

        for (int i = 0; i < count; i++)
        {
            float y = midY + Random.Range(-spread, spread);
            SpawnBird(new Vector3(transform.position.x, y));
        }
    }

    private void SpawnDiagonal(int minCount, int maxCount, float stepY)
    {
        int count = Random.Range(minCount, maxCount);
        float startY = Random.Range(minY, maxY);

        float direction = Random.value < 0.5f ? 1f : -1f;

        for (int i = 0; i < count; i++)
        {
            float y = startY + (i * stepY * direction);
            SpawnBird(new Vector3(transform.position.x, y));
        }
    }

    private void SpawnLine(int minCount, int maxCount, float spacing)
    {
        int count = Random.Range(minCount, maxCount);
        float startY = Random.Range(minY, maxY);

        for (int i = 0; i < count; i++)
        {
            float y = startY + (i * spacing);
            SpawnBird(new Vector3(transform.position.x, y));
        }
    }

    private void SpawnChaosSpray(int minCount, int maxCount)
    {
        int count = Random.Range(minCount, maxCount);

        for (int i = 0; i < count; i++)
        {
            float offsetX = Random.Range(-0.8f, 1.3f);
            float offsetY = Random.Range(minY, maxY);

            SpawnBird(new Vector3(transform.position.x + offsetX, offsetY));
        }
    }

    // ===========================================================
    // ====================== DEFENSE ============================
    // ===========================================================

    private void UpdateDefenseSpawning()
    {
        // continuous waves during defense
        defenseTimer += Time.deltaTime;
        defenseWaveTimer += Time.deltaTime;

        // decide which wave will carry the ball (early/mid/late-ish)
        if (defenseCarrierTargetWave == -1)
        {
            // Medium chaos: random between wave 2 and 6
            defenseCarrierTargetWave = Random.Range(2, 7);
        }

        if (defenseWaveTimer >= defenseWaveIntervalCurrent)
        {
            defenseWaveTimer = 0f;
            defenseWaveIntervalCurrent = Random.Range(defenseWaveIntervalMin, defenseWaveIntervalMax);

            defenseWavesSpawned++;

            bool shouldEmbedCarrier = false;

            if (!defenseCarrierSpawned)
            {
                // Prefer wave-based decision
                if (defenseWavesSpawned == defenseCarrierTargetWave)
                {
                    shouldEmbedCarrier = true;
                }
                else if (defenseTimer >= defenseCarrierDelay)
                {
                    // Failsafe: if we're past delay and still no carrier, embed now
                    shouldEmbedCarrier = true;
                }
            }

            SpawnDefenseWave(shouldEmbedCarrier);
        }
    }

    private void SpawnDefenseWave(bool includeCarrier)
    {
        if (cycloneBirdPrefab == null)
            return;

        // Pick a pattern like offense, but with slightly boosted counts
        int pattern = Random.Range(0, 4);

        List<Vector3> positions;

        switch (pattern)
        {
            case 0: // cluster
                positions = GenerateClusterPositions(8, 14, 0.7f);
                break;

            case 1: // diagonal
                positions = GenerateDiagonalPositions(8, 13, 0.5f);
                break;

            case 2: // line
                positions = GenerateLinePositions(10, 16, 0.55f);
                break;

            default: // chaos spray
                positions = GenerateChaosPositions(10, 18);
                break;
        }

        int carrierIndex = -1;

        if (includeCarrier && enemyBallCarrierPrefab != null && positions.Count > 0)
        {
            carrierIndex = Random.Range(0, positions.Count);

            GameObject carrier = Instantiate(enemyBallCarrierPrefab, positions[carrierIndex], Quaternion.identity);
            spawnedObjects.Add(carrier);
            defenseCarrierSpawned = true;
        }

        // Spawn birds at all other positions (skip the carrier index so it's not visually doubled)
        for (int i = 0; i < positions.Count; i++)
        {
            if (i == carrierIndex)
                continue;

            SpawnBird(positions[i]);
        }
    }

    // Defense pattern generators (return positions instead of spawning immediately)
    private List<Vector3> GenerateClusterPositions(int minCount, int maxCount, float spread)
    {
        List<Vector3> positions = new List<Vector3>();
        int count = Random.Range(minCount, maxCount);
        float midY = Random.Range(minY, maxY);

        for (int i = 0; i < count; i++)
        {
            float y = midY + Random.Range(-spread, spread);
            positions.Add(new Vector3(transform.position.x, y));
        }

        return positions;
    }

    private List<Vector3> GenerateDiagonalPositions(int minCount, int maxCount, float stepY)
    {
        List<Vector3> positions = new List<Vector3>();
        int count = Random.Range(minCount, maxCount);
        float startY = Random.Range(minY, maxY);

        float direction = Random.value < 0.5f ? 1f : -1f;

        for (int i = 0; i < count; i++)
        {
            float y = startY + (i * stepY * direction);
            positions.Add(new Vector3(transform.position.x, y));
        }

        return positions;
    }

    private List<Vector3> GenerateLinePositions(int minCount, int maxCount, float spacing)
    {
        List<Vector3> positions = new List<Vector3>();
        int count = Random.Range(minCount, maxCount);
        float startY = Random.Range(minY, maxY);

        for (int i = 0; i < count; i++)
        {
            float y = startY + (i * spacing);
            positions.Add(new Vector3(transform.position.x, y));
        }

        return positions;
    }

    private List<Vector3> GenerateChaosPositions(int minCount, int maxCount)
    {
        List<Vector3> positions = new List<Vector3>();
        int count = Random.Range(minCount, maxCount);

        for (int i = 0; i < count; i++)
        {
            float offsetX = Random.Range(-0.8f, 1.3f);
            float offsetY = Random.Range(minY, maxY);
            positions.Add(new Vector3(transform.position.x + offsetX, offsetY));
        }

        return positions;
    }

    // ===========================================================
    // ====================== HELPERS ============================
    // ===========================================================

    private void SpawnBird(Vector3 pos)
    {
        GameObject b = Instantiate(cycloneBirdPrefab, pos, Quaternion.identity);
        spawnedObjects.Add(b);
    }

    private void SpawnGoalPost()
    {
        if (goalPostPrefab == null || Camera.main == null)
            return;

        // spawn OFFSCREEN RIGHT like SP mode
        float x = Camera.main.ScreenToWorldPoint(
            new Vector3(Camera.main.pixelWidth, 0, 0)
        ).x + 1.2f;

        Vector3 spawnPos = new Vector3(x, 0.5f, 0f);

        GameObject post = Instantiate(goalPostPrefab, spawnPos, Quaternion.identity);
        spawnedObjects.Add(post);
    }

    public void SpawnFootball(Player carrier)
    {
        var ball = Instantiate(footballPrefab,
            carrier.transform.position + new Vector3(0.4f, -0.2f),
            Quaternion.identity);

        ball.GetComponent<MultiplayerFootball>()?.SetCarrier(carrier);
        spawnedObjects.Add(ball);
    }

    public void ResetSpawner()
    {
        // Offense
        spawnTimer = 0f;
        goalPostTimer = 0f;

        // Defense
        defenseTimer = 0f;
        defenseWaveTimer = 0f;
        defenseWaveIntervalCurrent = Random.Range(defenseWaveIntervalMin, defenseWaveIntervalMax);
        defenseWavesSpawned = 0;
        defenseCarrierSpawned = false;
        defenseCarrierTargetWave = -1;

        foreach (var o in spawnedObjects)
            if (o != null)
                Destroy(o);

        spawnedObjects.Clear();
    }
}
