// Spawner.cs
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject pipePrefab;
    public float spawnRate = 1.2f;
    public float minHeight = -1f;
    public float maxHeight = 2f;

    private float timer;

    private void OnEnable()
    {
        GameManager.OnSpawnRateChanged += HandleSpawnRateChanged;

        // Grab the current rate from GameManager (covers the “missed initial event” case)
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

        timer += Time.deltaTime;
        if (timer >= spawnRate)
        {
            timer = 0f;
            SpawnPipe();
        }
    }

    private void SpawnPipe()
    {
        GameObject pipes = Instantiate(pipePrefab, transform.position, Quaternion.identity);
        pipes.transform.position += Vector3.up * Random.Range(minHeight, maxHeight);
        GameManager.Instance?.RegisterPipe();
    }
}
