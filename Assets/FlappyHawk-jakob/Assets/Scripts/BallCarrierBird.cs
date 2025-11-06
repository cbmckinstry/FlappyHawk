using UnityEngine;

/// <summary>
/// BallCarrierBird - Enemy that carries the ball during defense rounds.
/// Player must collide with this enemy to win the defense round.
/// If this despawns without being hit, the opponent scores.
/// </summary>
public class BallCarrierBird : MonoBehaviour
{
    public float pipeSpeed = 4.5f;

    [Header("Flight Pattern")]
    [SerializeField] private float bobAmplitude = 0.5f;     // Vertical bob amplitude
    [SerializeField] private float bobFrequency = 1f;       // Speed of bobbing

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
        // Sync with global pipe speed
        pipeSpeed = GameManager.CurrentPipeSpeed;

        // Subscribe to speed updates
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
        // Mark as ball carrier so Player can recognize it
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
            Debug.LogWarning("BallCarrierBird has no flap sprites assigned!");

        // Notify GameDayManager that a ball carrier spawned
        GameManager.GameDayInstance?.OnBallCarrierSpawned();
    }

    private void Update()
    {
        // Move left
        transform.position += Vector3.left * pipeSpeed * Time.deltaTime;

        // Bob up and down slightly
        UpdateBobbing();

        // Animate wings
        UpdateFlapAnimation();

        // Despawn if off-screen
        if (transform.position.x < leftEdge)
        {
            // Destroy all cyclone birds (reset screen)
            foreach (var bird in FindObjectsByType<CycloneBird>(FindObjectsSortMode.None))
                Destroy(bird.gameObject);

            // Notify GameDayManager that it escaped (player loses defense)
            GameManager.GameDayInstance?.OnBallCarrierDespawned();

            Destroy(gameObject);
        }
    }

    private void UpdateBobbing()
    {
        bobTimer += Time.deltaTime;
        float bobOffset = Mathf.Sin(bobTimer * bobFrequency * Mathf.PI) * bobAmplitude;

        Vector3 pos = transform.position;
        pos.y = startYPosition + bobOffset;
        transform.position = pos;
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
            OnHitByPlayer();
        }
    }

    private void OnHitByPlayer()
    {
        // Destroy all cyclone birds in the scene
        foreach (var bird in FindObjectsByType<CycloneBird>(FindObjectsSortMode.None))
            Destroy(bird.gameObject);

        // Notify GameDayManager that the defense round was won
        GameManager.GameDayInstance?.EndDefenseRound(true);

        Destroy(gameObject);
    }
}
