using UnityEngine;

public class Helmet : MonoBehaviour, ICollectible
{
    public int healthGain = 1;
    public float moveSpeed = 4.5f;
    private float leftEdge;

    private void OnEnable()
    {
        // Sync with global scroll speed
        moveSpeed = GameManager.CurrentScrollSpeed;

        // Subscribe for updates when scroll speed changes
        GameManager.OnScrollSpeedChanged += HandleSpeedChanged;
    }

    private void OnDisable()
    {
        GameManager.OnScrollSpeedChanged -= HandleSpeedChanged;
    }

    private void HandleSpeedChanged(float newSpeed)
    {
        moveSpeed = newSpeed;
    }

    private void Start()
    {
        if (Camera.main == null)
        {
            Debug.LogError("No Main Camera found in scene!");
            return;
        }

        leftEdge = Camera.main.ScreenToWorldPoint(Vector3.zero).x - 1f;
    }

    private void Update()
    {
        // Move left with the global scroll speed
        transform.position += Vector3.left * moveSpeed * Time.deltaTime;

        // Destroy when off-screen
        if (transform.position.x < leftEdge)
            Destroy(gameObject);
    }

    public void Collect(Player player)
    {
        player.GainHealth(healthGain);
    }
}
