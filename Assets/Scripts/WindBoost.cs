using UnityEngine;

public class WindBoost : MonoBehaviour, ICollectible
{
    public int healthGain = 1;
    public float moveSpeed = 4.5f;
    private float leftEdge;
    private const float BOOST_DISTANCE = 1.5f;
    private const float BOOST_SPEED = 0.5f;

    private void OnEnable()
    {
        moveSpeed = GameManager.CurrentScrollSpeed;
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
        transform.position += Vector3.left * moveSpeed * Time.deltaTime;

        if (transform.position.x < leftEdge)
            Destroy(gameObject);
    }

    public void Collect(Player player)
    {
        bool isAtMaxHealth = player.GetHealth() >= player.GetMaxHealth();
        player.GainHealth(healthGain);
        
        if (!isAtMaxHealth)
        {
            player.ApplyHorizontalBoost(BOOST_DISTANCE, BOOST_SPEED);
        }
    }
}
