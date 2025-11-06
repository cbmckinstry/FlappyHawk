using UnityEngine;

/// <summary>
/// Tornado obstacle with two modes:
/// PARENT MODE: Stationary tornado at X=-9 that bobs vertically and emits small tornados
/// SMALL MODE: Traveling tornado that moves left-to-right across the screen
/// Only spawns in Hard mode
/// </summary>
public class Tornado : MonoBehaviour
{
    [Header("Tornado Type")]
    [SerializeField] private bool isParentTornado = true;  // Toggle between parent and small tornado behavior
    
    [Header("Parent Tornado Settings")]
    [SerializeField] private float parentSpawnX = -9f;     // X position where parent stays
    [SerializeField] private float parentMinY = -0.5f;     // Min bobbing height
    [SerializeField] private float parentMaxY = 1.5f;      // Max bobbing height
    [SerializeField] private float parentBobSpeed = 0.5f;  // Cycles per second for vertical bob
    [SerializeField] private float minEmissionInterval = 3f;  // Min time between spawning small tornados
    [SerializeField] private float maxEmissionInterval = 5f;  // Max time between spawning small tornados
    
    [Header("Small Tornado Settings")]
    [SerializeField] private float smallTornadoScale = 0.6f;     // Scale relative to parent
    [SerializeField] private float smallTornadoMinSpeed = 3f;    // Min speed left-to-right
    [SerializeField] private float smallTornadoMaxSpeed = 6f;    // Max speed left-to-right
    
    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 360f;     // Degrees per second
    
    [Header("Animation")]
    [SerializeField] private Sprite[] animationSprites;      // Array of sprites for animation
    [SerializeField] private float animationSpeed = 0.1f;    // Time between each frame
    
    private float leftEdge;
    private float rightEdge;
    private float startYPosition;
    private float currentRotation = 0f;
    private float animationTimer = 0f;
    private int currentAnimationFrame = 0;
    private Transform tornadoVisuals;
    private SpriteRenderer spriteRenderer;
    
    // Parent tornado only
    private float parentBobTimer = 0f;
    private float emissionTimer = 0f;
    private float nextEmissionTime = 0f;
    
    // Small tornado only
    private float horizontalSpeed = 0f;

    private void OnEnable()
    {
        if (!isParentTornado)
        {
            GameManager.OnPipeSpeedChanged += HandlePipeSpeedChanged;
        }
    }

    private void OnDisable()
    {
        if (!isParentTornado)
        {
            GameManager.OnPipeSpeedChanged -= HandlePipeSpeedChanged;
        }
    }

    private void HandlePipeSpeedChanged(float newSpeed)
    {
        // Small tornados move at their own speed, not affected by difficulty
    }

    private void Start()
    {
        gameObject.tag = "Obstacle";
        
        // Ensure collider is set up for collision detection
        CircleCollider2D collider = GetComponent<CircleCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<CircleCollider2D>();
        }
        collider.isTrigger = true; // Must be a trigger so Player.OnTriggerEnter2D() detects collision
        
        if (Camera.main == null)
        {
            Debug.LogError("No Main Camera found in scene!");
            return;
        }
        
        leftEdge = Camera.main.ScreenToWorldPoint(Vector3.zero).x - 1f;
        rightEdge = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, 0, 0)).x + 1f;
        
        // Find visuals for rotation
        tornadoVisuals = transform.Find("Visual");
        if (tornadoVisuals == null)
            tornadoVisuals = transform.Find("Visuals");
        if (tornadoVisuals == null)
            tornadoVisuals = transform;
        
        // Get sprite renderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        
        if (animationSprites == null || animationSprites.Length == 0)
        {
            Debug.LogWarning("Tornado has no animation sprites assigned!");
        }
        
        if (isParentTornado)
        {
            // Position parent tornado at fixed X, center Y
            transform.position = new Vector3(parentSpawnX, (parentMinY + parentMaxY) / 2f, 0);
            startYPosition = transform.position.y;
            nextEmissionTime = Random.Range(minEmissionInterval, maxEmissionInterval);
        }
        else
        {
            // Small tornado initialization
            startYPosition = transform.position.y;
            transform.localScale = Vector3.one * smallTornadoScale;
            horizontalSpeed = Random.Range(smallTornadoMinSpeed, smallTornadoMaxSpeed);
        }
    }

    private void Update()
    {
        if (isParentTornado)
        {
            UpdateParentTornado();
        }
        else
        {
            UpdateSmallTornado();
        }
        
        // Both types rotate and animate
        UpdateRotation();
        UpdateAnimation();
    }

    private void UpdateParentTornado()
    {
        // Bob up and down between parentMinY and parentMaxY
        parentBobTimer += Time.deltaTime;
        float midpoint = (parentMinY + parentMaxY) / 2f;
        float amplitude = (parentMaxY - parentMinY) / 2f;
        float newY = midpoint + Mathf.Sin(parentBobTimer * parentBobSpeed * Mathf.PI * 2f) * amplitude;
        
        transform.position = new Vector3(parentSpawnX, newY, 0);
        
        // Emit small tornados
        emissionTimer += Time.deltaTime;
        if (emissionTimer >= nextEmissionTime)
        {
            emissionTimer = 0f;
            nextEmissionTime = Random.Range(minEmissionInterval, maxEmissionInterval);
            SpawnSmallTornado();
        }
    }

    private void UpdateSmallTornado()
    {
        // Move horizontally from left to right
        transform.position += Vector3.right * horizontalSpeed * Time.deltaTime;
        
        // Destroy when off screen to the right
        if (transform.position.x > rightEdge)
            Destroy(gameObject);
    }

    private void UpdateRotation()
    {
        currentRotation += rotationSpeed * Time.deltaTime;
        if (currentRotation >= 360f)
            currentRotation -= 360f;

        if (tornadoVisuals != null)
            tornadoVisuals.rotation = Quaternion.AngleAxis(currentRotation, Vector3.forward);
    }

    private void UpdateAnimation()
    {
        if (animationSprites == null || animationSprites.Length == 0)
            return;

        animationTimer += Time.deltaTime;

        if (animationTimer >= animationSpeed)
        {
            animationTimer = 0f;
            currentAnimationFrame = (currentAnimationFrame + 1) % animationSprites.Length;
            spriteRenderer.sprite = animationSprites[currentAnimationFrame];
        }
    }
    
    private void SpawnSmallTornado()
    {
        // Random height within parent's range
        float randomHeight = Random.Range(parentMinY, parentMaxY);
        
        // Clone this tornado as a small one
        GameObject smallTornadoGO = Instantiate(gameObject, new Vector3(leftEdge, randomHeight, 0), Quaternion.identity);
        Tornado smallTornadoScript = smallTornadoGO.GetComponent<Tornado>();
        
        if (smallTornadoScript != null)
        {
            smallTornadoScript.InitializeSmallTornado();
        }
    }
    
    private void InitializeSmallTornado()
    {
        isParentTornado = false;
        transform.localScale = Vector3.one * smallTornadoScale;
        horizontalSpeed = Random.Range(smallTornadoMinSpeed, smallTornadoMaxSpeed);
    }
}