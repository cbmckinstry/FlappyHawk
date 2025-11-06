using UnityEngine;

/// <summary>
/// BallCarrierBird - An enemy that carries the ball during defense rounds
/// Player must collide with this enemy to win the defense round
/// If this despawns without being hit, the opponent scores
/// </summary>
public class BallCarrierBird : MonoBehaviour
{
    public float pipeSpeed = 4.5f;
    
    [Header("Flight Pattern")]
    [SerializeField] private float bobAmplitude = .5f;      // Noticeable bobbing for ball carrier
    [SerializeField] private float bobFrequency = 1f;        // Speed of bobbing
    
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
        // Set speed to whatever GameManager currently uses
        var gm = FindObjectOfType<GameManager>();
        if (gm != null) pipeSpeed = gm.CurrentPipeSpeed;

        // Subscribe to future difficulty changes
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
        // Mark as a ball carrier so player can interact with it to win defense
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
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        
        if (flapSprites == null || flapSprites.Length == 0)
        {
            Debug.LogWarning("BallCarrierBird has no flap sprites assigned!");
        }

        // Notify GameDayManager that ball carrier spawned
        GameDayManager gameDayMgr = FindObjectOfType<GameDayManager>();
        if (gameDayMgr != null)
        {
            gameDayMgr.OnBallCarrierSpawned();
        }
    }

    private void Update()
    {
        // Move left with the game
        transform.position += Vector3.left * pipeSpeed * Time.deltaTime;

        // Apply subtle bobbing motion
        UpdateBobbing();

        // Update flapping animation
        UpdateFlapAnimation();

        // Destroy when off screen
        if (transform.position.x < leftEdge)
        {
            // Destroy all enemy birds when ball carrier escapes
            CycloneBird[] cycloneBirds = FindObjectsOfType<CycloneBird>();
            foreach (CycloneBird bird in cycloneBirds)
            {
                Destroy(bird.gameObject);
            }
            
            // Notify GameDayManager that ball carrier despawned without being hit
            GameDayManager gameDayMgr = FindObjectOfType<GameDayManager>();
            if (gameDayMgr != null)
            {
                gameDayMgr.OnBallCarrierDespawned();
            }
            
            Destroy(gameObject);
        }
    }

    private void UpdateBobbing()
    {
        // Subtle sinusoidal bobbing motion
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
        // Destroy all enemy birds on screen
        CycloneBird[] cycloneBirds = FindObjectsOfType<CycloneBird>();
        foreach (CycloneBird bird in cycloneBirds)
        {
            Destroy(bird.gameObject);
        }
        
        // Notify GameDayManager that defense round ended successfully
        GameDayManager gameDayMgr = FindObjectOfType<GameDayManager>();
        if (gameDayMgr != null)
        {
            gameDayMgr.EndDefenseRound(true); // true = player won defense
        }
        
        // Destroy this enemy
        Destroy(gameObject);
    }
}