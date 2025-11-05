using UnityEngine;
using UnityEngine.InputSystem;

public class Football : MonoBehaviour
{
    private Player player;
    private bool isCarried = false;
    private Vector3 carryOffset = Vector3.zero;
    public float moveSpeed = 4.5f;
    private float leftEdge;

    // Screen bounds for off-screen detection
    private float screenLeft;
    private float screenRight;
    private float screenTop;
    private float screenBottom;

    private void OnEnable()
    {
        // Sync with global pipe speed
        moveSpeed = GameManager.CurrentPipeSpeed;

        // Subscribe to global speed changes
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
        if (isCarried && player != null)
        {
            // Follow player
            transform.position = player.transform.position + carryOffset;

            // If player carrying ball goes off-screen, trigger defense
            if (IsPlayerOffScreen())
            {
                Debug.Log("[Football] Player carrying ball went off-screen — entering Defense mode.");
                isCarried = false;

                var gameDayMgr = GameManager.GameDayInstance;
                if (gameDayMgr != null && !gameDayMgr.IsInDefenseRound())
                    gameDayMgr.StartDefenseRound();

                Destroy(gameObject);
                return;
            }

            // Drop football manually with 'E'
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
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
                        Debug.Log("[Football] Ball despawned during defense — opponent scores!");
                        gameDayMgr.EndDefenseRound(false);
                    }
                    else
                    {
                        Debug.Log("[Football] Ball despawned in offense — switching to defense.");
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
        if (!isCarried && other.CompareTag("Player"))
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

        if (player != null)
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f;
        }
    }

    public bool IsCarried() => isCarried;
}
