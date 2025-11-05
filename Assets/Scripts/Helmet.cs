using UnityEngine;

public class Helmet : MonoBehaviour, ICollectible
{
    public int healthGain = 1;
    public float moveSpeed = 4.5f;
    private float leftEdge;

    private void OnEnable()
    {
        // Sync with global pipe speed
        moveSpeed = GameManager.CurrentPipeSpeed;

        // Subscribe for updates when pipe speed changes
        GameManager.OnPipeSpeedChanged += HandleSpeedChanged;
    }

    private void OnDisable()
    {
        GameManager.OnPipeSpeedChanged -= HandleSpeedChanged;
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
        // Move left with the global pipe speed
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
