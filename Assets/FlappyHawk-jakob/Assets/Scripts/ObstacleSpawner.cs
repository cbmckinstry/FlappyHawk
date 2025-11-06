using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Common Settings")]
    public float spawnInterval = 1.5f;
    public float moveSpeed = 4f;
    public float xSpawnPosition = 10f;
    public float destroyX = -12f;

    [Header("Iowa Mode Prefabs")]
    public GameObject siloPrefab;
    public GameObject windmillPrefab;
    public GameObject balloonPrefab;
    public GameObject tornadoPrefab; // Hard mode only

    [Header("Gameday Mode Prefabs")]
    public GameObject cyclonePrefab;

    private float timer = 0f;

    private void Update()
    {
        if (!GameManager.IsGameActive()) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnObstacle();
            timer = 0f;
        }
    }

    private void SpawnObstacle()
    {
        GameObject prefabToSpawn = null;

        if (GameManager.CurrentGameMode == GameManager.GameMode.Iowa)
        {
            prefabToSpawn = GetRandomIowaObstacle();
        }
        else if (GameManager.CurrentGameMode == GameManager.GameMode.GameDay)
        {
            prefabToSpawn = cyclonePrefab;
        }

        if (prefabToSpawn == null) return;

        Vector3 spawnPos = new Vector3(xSpawnPosition, RandomY(), 0);
        GameObject obstacle = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);

        ObstacleMovement mover = obstacle.AddComponent<ObstacleMovement>();
        mover.speed = moveSpeed;
        mover.destroyX = destroyX;
    }

    private GameObject GetRandomIowaObstacle()
    {
        // Tornado spawns only in Hard difficulty
        if (GameManager.CurrentDifficulty == GameManager.Difficulty.Hard && Random.value < 0.2f && tornadoPrefab != null)
            return tornadoPrefab;

        float roll = Random.value;
        if (roll < 0.33f && siloPrefab != null) return siloPrefab;
        if (roll < 0.66f && windmillPrefab != null) return windmillPrefab;
        if (balloonPrefab != null) return balloonPrefab;

        return null;

    }

    private float RandomY()
    {
        // Simple range for variation; tweak per mode
        if (GameManager.CurrentGameMode == GameManager.GameMode.Iowa)
            return Random.Range(-1.5f, 2.0f);
        else
            return Random.Range(-2.0f, 2.5f);
    }
}
