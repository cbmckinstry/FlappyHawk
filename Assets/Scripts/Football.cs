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
        // Subscribe to pipe speed changes for consistency
        var gm = FindObjectOfType<GameManager>();
        if (gm != null) moveSpeed = gm.CurrentPipeSpeed;
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
        
        // Calculate screen bounds for off-screen detection
        Vector3 bottomLeft = Camera.main.ScreenToWorldPoint(Vector3.zero);
        Vector3 topRight = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, Camera.main.pixelHeight));
        
        screenLeft = bottomLeft.x - 1f;
        screenRight = topRight.x + 1f;
        screenBottom = bottomLeft.y - 1f;
        screenTop = topRight.y + 1f;
        
        // Try to auto-attach to player
        player = FindObjectOfType<Player>();
    }

    private void Update()
    {
        if (isCarried && player != null)
        {
            // Follow player
            transform.position = player.transform.position + carryOffset;

            // Check if player (carrying ball) has gone off-screen
            if (IsPlayerOffScreen())
            {
                Debug.Log("[Football] Player carrying ball went off-screen! Despawning ball and entering Defense mode.");
                isCarried = false;
                
                // Notify GameDayManager to start defense round
                GameDayManager gameDayMgr = FindObjectOfType<GameDayManager>();
                if (gameDayMgr != null && !gameDayMgr.IsInDefenseRound())
                {
                    gameDayMgr.StartDefenseRound();
                }
                
                Destroy(gameObject);
                return;
            }

            // Check for drop input (E key)
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                Drop();
            }
        }
        else
        {
            // Move left with the game (when not carried)
            transform.position += Vector3.left * moveSpeed * Time.deltaTime;
            
            // Destroy when off screen
            if (transform.position.x < leftEdge)
            {
                GameDayManager gameDayMgr = FindObjectOfType<GameDayManager>();
                if (gameDayMgr != null)
                {
                    if (gameDayMgr.IsInDefenseRound())
                    {
                        Debug.Log("[Football] Ball despawned without being collected during defense!");
                        gameDayMgr.EndDefenseRound(false); // Opponent scores
                    }
                    else
                    {
                        // Ball despawned during offense mode without being grabbed - enter defense
                        Debug.Log("[Football] Ball despawned without being collected during offense! Entering Defense mode.");
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
        
        return (player.transform.position.x < screenLeft || 
                player.transform.position.x > screenRight || 
                player.transform.position.y < screenBottom || 
                player.transform.position.y > screenTop);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isCarried && other.CompareTag("Player"))
        {
            player = other.GetComponent<Player>();
            if (player != null)
            {
                Carry();
            }
        }
    }

    public void Carry()
    {
        isCarried = true;
        carryOffset = new Vector3(0f, -0.35f, -0.1f); // Position at bottom left talons, on top of sprite
        gameObject.tag = "Collectible"; // Tag it as collectible so it won't interfere
    }

    public void Drop()
    {
        isCarried = false;
        if (player != null)
        {
            // Enable gravity so the football falls
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
            }
            rb.isKinematic = false;
            rb.gravityScale = 1f;
        }
    }

    public bool IsCarried()
    {
        return isCarried;
    }
}