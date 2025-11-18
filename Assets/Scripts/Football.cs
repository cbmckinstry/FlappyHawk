using UnityEngine;
using UnityEngine.InputSystem;

public class Football : MonoBehaviour
{
    private Player player;
    private bool isCarried = false;
    private Vector3 carryOffset = Vector3.zero;
    public float moveSpeed = 4.5f;
    private float leftEdge;
    private float dropInvulnerabilityTimer = 0f;
    private const float DROP_INVULNERABILITY_DURATION = 0.5f;

    // Screen bounds for off-screen detection
    private float screenLeft;
    private float screenRight;
    private float screenTop;
    private float screenBottom;

    private void OnEnable()
    {
        // Sync with global scroll speed
        moveSpeed = GameManager.CurrentScrollSpeed;

        // Subscribe to global speed changes
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

        // Cache screen bounds for off-screen checks
        Vector3 bottomLeft = Camera.main.ScreenToWorldPoint(Vector3.zero);
        Vector3 topRight = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, Camera.main.pixelHeight));
        screenLeft = bottomLeft.x - 1f;
        screenRight = topRight.x + 1f;
        screenBottom = bottomLeft.y - 1f;
        screenTop = topRight.y + 1f;

        // Try to find the player
        player = FindFirstObjectByType<Player>();
    }

    private void Update()
    {
        if (dropInvulnerabilityTimer > 0f)
            dropInvulnerabilityTimer -= Time.deltaTime;

        if (isCarried && player != null)
        {
            // Follow player
            transform.position = player.transform.position + carryOffset;

            // If player carrying ball goes off-screen, trigger defense
            if (IsPlayerOffScreen())
            {
                Debug.Log("[Football] Player carrying ball went off-screen � entering Defense mode.");
                isCarried = false;

                var gameDayMgr = GameManager.GameDayInstance;
                if (gameDayMgr != null && !gameDayMgr.IsInDefenseRound())
                    gameDayMgr.StartDefenseRound();

                Destroy(gameObject);
                return;
            }

            // Drop football manually with 'E' or controller bumpers
            bool dropKey = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
            bool dropController = ControllerInputManager.Instance != null && ControllerInputManager.Instance.IsBallDropPressed();
            
            if (dropKey || dropController)
                Drop();
        }
        else
        {
            // Move left when not carried
            transform.position += Vector3.left * moveSpeed * Time.deltaTime;

            // Destroy if off screen
            if (transform.position.x < leftEdge)
            {
                var gameDayMgr = GameManager.GameDayInstance;
                if (gameDayMgr != null)
                {
                    if (gameDayMgr.IsInDefenseRound())
                    {
                        Debug.Log("[Football] Ball despawned during defense � opponent scores!");
                        gameDayMgr.EndDefenseRound(false);
                    }
                    else
                    {
                        Debug.Log("[Football] Ball despawned in offense � switching to defense.");
                        gameDayMgr.StartDefenseRound();
                    }
                }

                Destroy(gameObject);
            }
        }
    }

    private bool IsPlayerOffScreen()
    {
        if (player == null) return false;

        Vector3 pos = player.transform.position;
        return pos.x < screenLeft || pos.x > screenRight || pos.y < screenBottom || pos.y > screenTop;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isCarried && other.CompareTag("Player") && dropInvulnerabilityTimer <= 0f)
        {
            player = other.GetComponent<Player>();
            if (player != null)
                Carry();
        }
    }

    public void Carry()
    {
        isCarried = true;
        carryOffset = new Vector3(0f, -0.35f, -0.1f);
        gameObject.tag = "Collectible";
    }

    public void Drop()
{
    isCarried = false;
    dropInvulnerabilityTimer = DROP_INVULNERABILITY_DURATION;

    if (player != null)
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 1.5f;

        rb.linearVelocity = Vector2.zero;

        float forwardForce = 3f;
        float downwardForce = 2f;
        Vector2 dropDirection = new Vector2(1f, -1f).normalized;

        rb.AddForce(dropDirection * new Vector2(forwardForce, downwardForce).magnitude, ForceMode2D.Impulse);
    }
}


    public bool IsCarried() => isCarried;
}
