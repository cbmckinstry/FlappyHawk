using UnityEngine;

/// <summary>
/// EnemyBallCarrier - An enemy that carries the ball during defense rounds.
/// Player must collide with this enemy to begin the offense round again.
/// </summary>
public class EnemyBallCarrier : MonoBehaviour
{
    public float pipeSpeed = 4.5f;

    [Header("Flight Pattern")]
    [SerializeField] private float bobAmplitude = 0.5f;
    [SerializeField] private float bobFrequency = 2f;

    [Header("Animation")]
    [SerializeField] private Sprite[] flapSprites;
    [SerializeField] private float flapSpeed = 0.1f;

    private float leftEdge;
    private float startYPosition;
    private float bobTimer = 0f;
    private float flapTimer = 0f;
    private int currentFlapFrame = 0;
    private SpriteRenderer spriteRenderer;
    private bool hasBeenHit = false;

    private void OnEnable()
    {
        // Use static bridge for current pipe speed
        pipeSpeed = GameManager.CurrentPipeSpeed;

        // Subscribe to difficulty/speed changes
        GameManager.OnPipeSpeedChanged += HandlePipeSpeedChanged;
    }

    private void OnDisable()
    {
        GameManager.OnPipeSpeedChanged -= HandlePipeSpeedChanged;
    }

    private void HandlePipeSpeedChanged(float newSpeed)
    {
        pipeSpeed = newSpeed;
    }

    private void Start()
    {
        gameObject.tag = "BallCarrier";

        if (Camera.main == null)
        {
            Debug.LogError("No Main Camera found in scene!");
            return;
        }

        leftEdge = Camera.main.ScreenToWorldPoint(Vector3.zero).x - 1f;
        startYPosition = transform.position.y;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        if (flapSprites == null || flapSprites.Length == 0)
            Debug.LogWarning("EnemyBallCarrier has no flap sprites assigned!");
    }

    private void Update()
    {
        // Move left with the game
        transform.position += Vector3.left * pipeSpeed * Time.deltaTime;

        // Optional bobbing motion
        bobTimer += Time.deltaTime * bobFrequency;
        float bobOffset = Mathf.Sin(bobTimer) * bobAmplitude;
        transform.position = new Vector3(transform.position.x, startYPosition + bobOffset, transform.position.z);

        // Update flapping animation
        UpdateFlapAnimation();

        // Destroy when off screen
        if (transform.position.x < leftEdge)
            Destroy(gameObject);
    }

    private void UpdateFlapAnimation()
    {
        if (flapSprites == null || flapSprites.Length == 0)
            return;

        flapTimer += Time.deltaTime;

        if (flapTimer >= flapSpeed)
        {
            flapTimer = 0f;
            currentFlapFrame = (currentFlapFrame + 1) % flapSprites.Length;
            spriteRenderer.sprite = flapSprites[currentFlapFrame];
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasBeenHit) return;

        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            hasBeenHit = true;
            EndDefenseRound();
        }
    }

    private void EndDefenseRound()
    {
        // Notify the GameDayManager that the player won defense
        var gameDayMgr = GameManager.GameDayInstance;
        if (gameDayMgr != null)
        {
            gameDayMgr.EndDefenseRound(true);
        }

        Destroy(gameObject);
    }
}
