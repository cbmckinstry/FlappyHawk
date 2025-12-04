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
    public float defenseCarrierDelay = 3f;

    private float spawnTimer = 0f;
    private float goalPostTimer = 0f;
    private const float GOALPOST_RATE = 8f;

    private bool defenseCarrierSpawned = false;
    private float defenseTimer = 0f;

    private MultiplayerManager mp => MultiplayerManager.Instance;

    private List<GameObject> spawnedObjects = new List<GameObject>();


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
        int type = Random.Range(0, 3);

        switch (type)
        {
            case 0: SpawnCluster(3, 6, 0.45f); break;
            case 1: SpawnDiagonal(3, 5, 0.35f); break;
            case 2: SpawnLine(3, 6, 0.4f); break;
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

    // ===========================================================
    // ====================== DEFENSE ============================
    // ===========================================================
    private void UpdateDefenseSpawning()
    {
        defenseTimer += Time.deltaTime;
        spawnTimer += Time.deltaTime;

        if (!defenseCarrierSpawned)
        {
            if (spawnTimer >= spawnRate)
            {
                spawnTimer = 0f;
                SpawnDefenseCluster();
            }

            if (defenseTimer >= defenseCarrierDelay)
            {
                SpawnHiddenCarrierCluster();
                defenseCarrierSpawned = true;
            }
        }
    }

    private void SpawnDefenseCluster()
    {
        SpawnCluster(5, 9, 0.6f);
    }

    private void SpawnHiddenCarrierCluster()
    {
        float midY = Random.Range(minY, maxY);

        // fake clusters around it
        int fakeClusters = Random.Range(1, 3);
        for (int i = 0; i < fakeClusters; i++)
        {
            SpawnCluster(3, 6, 0.45f);
        }

        // spawn the real carrier buried in birds
        Vector3 carrierPos = new Vector3(transform.position.x, midY);
        GameObject real = Instantiate(enemyBallCarrierPrefab, carrierPos, Quaternion.identity);
        spawnedObjects.Add(real);

        // spawn birds around the carrier
        for (int i = 0; i < Random.Range(4, 7); i++)
        {
            float y = midY + Random.Range(-0.6f, 0.6f);
            SpawnBird(new Vector3(transform.position.x, y));
        }
    }

    // ===========================================================
    // HELPERS
    // ===========================================================
    private void SpawnBird(Vector3 pos)
    {
        GameObject b = Instantiate(cycloneBirdPrefab, pos, Quaternion.identity);
        spawnedObjects.Add(b);
    }

    private void SpawnGoalPost()
    {
        GameObject post = Instantiate(goalPostPrefab,
            new Vector3(transform.position.x, 0.5f), Quaternion.identity);

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
        spawnTimer = goalPostTimer = defenseTimer = 0f;
        defenseCarrierSpawned = false;

        foreach (var o in spawnedObjects)
            if (o != null) Destroy(o);

        spawnedObjects.Clear();
    }
}
