using UnityEngine;

/// <summary>
/// CycloneBird obstacle - A flying enemy with flapping animation
/// Moves left while bobbing up and down in a flight pattern
/// </summary>
public class CycloneBird : MonoBehaviour
{
    public float pipeSpeed = 4.5f;
    
    [Header("Flight Pattern")]
    [SerializeField] private float bobAmplitude = 0.5f;      // How far up/down it bobs
    [SerializeField] private float bobFrequency = 2f;        // Speed of bobbing (cycles per second)
    
    [Header("Animation")]
    [SerializeField] private Sprite[] flapSprites;           // Array of sprites for flapping animation
    [SerializeField] private float flapSpeed = 0.1f;         // Time between each frame
    
    private float leftEdge;
    private float startYPosition;
    private float bobTimer = 0f;
    private float flapTimer = 0f;
    private int currentFlapFrame = 0;
    private SpriteRenderer spriteRenderer;

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
        // Ensure the tag is set correctly for collision detection
        gameObject.tag = "Obstacle";
        
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
            Debug.LogWarning("CycloneBird has no flap sprites assigned!");
        }
    }

    private void Update()
    {
        // Move left with the game
        transform.position += Vector3.left * pipeSpeed * Time.deltaTime;

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
}