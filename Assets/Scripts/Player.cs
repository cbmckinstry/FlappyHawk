using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    // Movement
    private Vector3 direction;
    public float gravity = -9.8f;
    public float strength = 1f;

    // Flying animation
    private SpriteRenderer spriteRenderer;
    public Sprite[] flyingSprites;
    private int spriteIndex = 0;
    public float animationSpeed = 0.15f;

    // Health system
    private int health = 1;
    public int maxHealth = 3;

    // Helmet display
    private GameObject helmetDisplay;
    public bool hasHelmet { get; private set; } = false;

    // Screen bounds for off-screen detection
    private float screenLeft;
    private float screenRight;
    private float screenTop;
    private float screenBottom;
    private bool hasLeftScreen = false;

    private void AnimateSprite()
    {
        // Cycle through flying sprites for flapping animation
        spriteIndex++;
        if (spriteIndex >= flyingSprites.Length)
        {
            spriteIndex = 0;
        }
        spriteRenderer.sprite = flyingSprites[spriteIndex];
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Find helmet display child object (should be named "HelmetDisplay")
        helmetDisplay = transform.Find("HelmetDisplay")?.gameObject;
        if (helmetDisplay != null)
        {
            helmetDisplay.SetActive(false); // Hidden by default
        }

        // Calculate screen bounds for off-screen detection
        if (Camera.main != null)
        {
            Vector3 bottomLeft = Camera.main.ScreenToWorldPoint(Vector3.zero);
            Vector3 topRight = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, Camera.main.pixelHeight));
            
            screenLeft = bottomLeft.x - 1f;
            screenRight = topRight.x + 1f;
            screenBottom = bottomLeft.y - 1f;
            screenTop = topRight.y + 1f;
        }
    }

    private void Start()
    {
        // Animate wings based on animation speed
        InvokeRepeating(nameof(AnimateSprite), animationSpeed, animationSpeed);
    }

    private void OnEnable()
    {
        // Reset position & velocity & health
        Vector3 position = transform.position;
        position.y = 0f;
        transform.position = position;
        direction = Vector3.zero;
        health = 1;
        hasHelmet = false;
        hasLeftScreen = false; // Reset off-screen flag
        if (helmetDisplay != null)
            helmetDisplay.SetActive(false);
    }

    private void Update(){
    bool flap =
        (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
        (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
        (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);

    if (flap)
        direction = Vector3.up * strength;

    direction.y += gravity * Time.deltaTime;
    transform.position += direction * Time.deltaTime;

    // Check if player has gone off-screen
    CheckOffScreenAndTriggerDefense();
    }

    private void CheckOffScreenAndTriggerDefense()
    {
        bool isOffScreen = (transform.position.x < screenLeft || 
                           transform.position.x > screenRight || 
                           transform.position.y < screenBottom || 
                           transform.position.y > screenTop);

        // If player just left the screen, trigger Defense mode in Game Day
        if (isOffScreen && !hasLeftScreen)
        {
            hasLeftScreen = true;
            Debug.Log($"[Player] Ball went off-screen at position: {transform.position}");
            
            GameManager gm = FindObjectOfType<GameManager>();
            if (gm != null && gm.CurrentGameMode == GameMode.GameDay)
            {
                GameDayManager gameDayMgr = FindObjectOfType<GameDayManager>();
                if (gameDayMgr != null && !gameDayMgr.IsInDefenseRound())
                {
                    Debug.Log("[Player] Triggering Defense mode - ball went off-screen!");
                    gameDayMgr.StartDefenseRound();
                }
            }
        }
        else if (!isOffScreen)
        {
            hasLeftScreen = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Obstacle"))
        {
            TakeDamage();
        }
        else if (other.gameObject.CompareTag("Scoring"))
        {
            FindObjectOfType<GameManager>().IncreaseScore();
        }
        else if (other.gameObject.CompareTag("Collectible"))
        {
            HandleCollectible(other.gameObject);
        }
    }

    private void TakeDamage()
    {
        health--;
        FindObjectOfType<GameManager>().OnPlayerDamaged(health);
        
        // Hide helmet if health drops to 1 (losing helmet protection)
        if (health == 1)
        {
            hasHelmet = false;
            if (helmetDisplay != null)
                helmetDisplay.SetActive(false);
        }
        
        if (health <= 0)
        {
            FindObjectOfType<GameManager>().GameOver();
        }
    }

    private void HandleCollectible(GameObject collectible)
    {
        ICollectible col = collectible.GetComponent<ICollectible>();
        if (col != null)
        {
            col.Collect(this);
            Destroy(collectible);
        }
    }

    public void GainHealth(int amount)
    {
        health = Mathf.Min(health + amount, maxHealth);
        
        // Show helmet if health is now above 1
        if (health > 1)
        {
            hasHelmet = true;
            if (helmetDisplay != null)
                helmetDisplay.SetActive(true);
        }
        
        FindObjectOfType<GameManager>().OnPlayerHealed(health);
    }

    public int GetHealth()
    {
        return health;
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }
}