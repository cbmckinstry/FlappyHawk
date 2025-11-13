using UnityEngine;

public class CornKernel : MonoBehaviour, ICollectible
{
    public int pointsValue = 1;
    public float moveSpeed = 4.5f;
    private float leftEdge;

    private void OnEnable()
    {
        // Sync with global scroll speed
        moveSpeed = GameManager.CurrentScrollSpeed;

        // Subscribe to scroll speed changes for consistency
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
        // Move left with the game
        transform.position += Vector3.left * moveSpeed * Time.deltaTime;

        // Destroy when off screen
        if (transform.position.x < leftEdge)
            Destroy(gameObject);
    }

    public void Collect(Player player)
    {
        AudioManager.Instance?.PlayCornCollect();

        // Award points through global GameManager
        if (GameManager.IowaInstance != null)
            GameManager.IowaInstance.IncreaseScore(pointsValue);
        else if (GameManager.GameDayInstance != null)
            GameManager.GameDayInstance.IncreaseScore(pointsValue);
    }
}
