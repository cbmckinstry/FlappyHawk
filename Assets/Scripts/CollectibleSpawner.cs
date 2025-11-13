using UnityEngine;

public class CollectibleSpawner : MonoBehaviour
{
    [Header("Common Settings")]
    public float spawnInterval = 1.5f;
    public float moveSpeed = 3f;
    public float xSpawnPosition = 10f;
    public float destroyX = -12f;

    [Header("Iowa Collectible")]
    public GameObject cornPrefab;

    [Header("Gameday Collectible")]
    public GameObject footballPrefab;

    private float timer;

    private void Update()
    {
        if (!GameManager.IsGameActive()) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnCollectible();
            timer = 0f;
        }
    }

    private void SpawnCollectible()
    {
        GameObject prefab = null;

        if (GameManager.CurrentGameMode == GameManager.GameMode.Iowa)
            prefab = cornPrefab;
        else if (GameManager.CurrentGameMode == GameManager.GameMode.GameDay)
            prefab = footballPrefab;

        if (prefab == null) return;

        Vector3 spawnPos = new Vector3(xSpawnPosition, RandomY(), 0);
        GameObject collectible = Instantiate(prefab, spawnPos, Quaternion.identity);

        CollectibleMovement mover = collectible.AddComponent<CollectibleMovement>();
        mover.speed = moveSpeed;
        mover.destroyX = destroyX;
    }

    private float RandomY()
    {
        if (GameManager.CurrentGameMode == GameManager.GameMode.Iowa)
            return Random.Range(-1.5f, 2.0f);
        else
            return Random.Range(-2.0f, 2.5f);
    }
}
