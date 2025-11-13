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
    [SerializeField] public bool isParentTornado = true; // made public so it can be set before Awake()

    [Header("Parent Tornado Settings")]
    [SerializeField] private float parentSpawnX = -9f;
    [SerializeField] private float parentMinY = -0.5f;
    [SerializeField] private float parentMaxY = 1.5f;
    [SerializeField] private float parentBobSpeed = 0.5f;
    [SerializeField] private float minEmissionInterval = 3f;
    [SerializeField] private float maxEmissionInterval = 5f;

    [Header("Small Tornado Settings")]
    [SerializeField] private float smallTornadoScale = 0.6f;
    [SerializeField] private float smallTornadoMinSpeed = 3f;
    [SerializeField] private float smallTornadoMaxSpeed = 6f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 360f;

    [Header("Animation")]
    [SerializeField] private Sprite[] animationSprites;
    [SerializeField] private float animationSpeed = 0.1f;

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

    private void Start()
    {
        gameObject.tag = "Obstacle";

        // Prevent duplicate *parent* tornados only
        if (isParentTornado)
        {
            Tornado[] allTornados = FindObjectsOfType<Tornado>();
            foreach (var t in allTornados)
            {
                if (t != this && t.isParentTornado)
                {
                    Debug.LogWarning("[Tornado] Duplicate parent tornado detected. Destroying extra instance.");
                    Destroy(gameObject);
                    return;
                }
            }
        }

        // Ensure collider exists
        CircleCollider2D collider = GetComponent<CircleCollider2D>();
        if (collider == null)
            collider = gameObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;

        if (Camera.main == null)
        {
            Debug.LogError("No Main Camera found in scene!");
            return;
        }

        leftEdge = Camera.main.ScreenToWorldPoint(Vector3.zero).x - 1f;
        rightEdge = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, 0, 0)).x + 1f;

        tornadoVisuals = transform.Find("Visual") ?? transform.Find("Visuals") ?? transform;
        spriteRenderer = GetComponent<SpriteRenderer>() ?? gameObject.AddComponent<SpriteRenderer>();

        if (animationSprites == null || animationSprites.Length == 0)
            Debug.LogWarning("[Tornado] No animation sprites assigned!");

        if (isParentTornado)
        {
            transform.position = new Vector3(parentSpawnX, (parentMinY + parentMaxY) / 2f, 0);
            startYPosition = transform.position.y;
            nextEmissionTime = Random.Range(minEmissionInterval, maxEmissionInterval);
        }
        else
        {
            InitializeSmallTornado();
        }
    }

    private void Update()
    {
        if (isParentTornado)
            UpdateParentTornado();
        else
            UpdateSmallTornado();

        UpdateRotation();
        UpdateAnimation();
    }

    private void UpdateParentTornado()
    {
        parentBobTimer += Time.deltaTime;
        float midpoint = (parentMinY + parentMaxY) / 2f;
        float amplitude = (parentMaxY - parentMinY) / 2f;
        float newY = midpoint + Mathf.Sin(parentBobTimer * parentBobSpeed * Mathf.PI * 2f) * amplitude;
        transform.position = new Vector3(parentSpawnX, newY, 0);

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
        transform.position += Vector3.right * horizontalSpeed * Time.deltaTime;
        if (transform.position.x > rightEdge)
            Destroy(gameObject);
    }

    private void UpdateRotation()
    {
        currentRotation += rotationSpeed * Time.deltaTime;
        if (currentRotation >= 360f)
            currentRotation -= 360f;
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
        float randomHeight = Random.Range(parentMinY, parentMaxY);

        // Instantiate and pre-mark as small BEFORE Start() runs
        GameObject smallTornadoGO = Instantiate(gameObject, new Vector3(leftEdge, randomHeight, 0), Quaternion.identity);
        Tornado smallTornadoScript = smallTornadoGO.GetComponent<Tornado>();
        if (smallTornadoScript != null)
        {
            smallTornadoScript.isParentTornado = false; // ensure this is set before Start()
            smallTornadoScript.InitializeSmallTornado();
        }
    }

    public void InitializeSmallTornado()
    {
        isParentTornado = false;
        transform.localScale = Vector3.one * smallTornadoScale;
        horizontalSpeed = Random.Range(smallTornadoMinSpeed, smallTornadoMaxSpeed);
    }

    private void OnDestroy()
    {
        // Clean up spawned small tornados if this parent is destroyed
        if (isParentTornado)
        {
            Tornado[] tornados = FindObjectsOfType<Tornado>();
            foreach (var t in tornados)
            {
                if (t != this && !t.isParentTornado)
                    Destroy(t.gameObject);
            }
        }
    }
}
