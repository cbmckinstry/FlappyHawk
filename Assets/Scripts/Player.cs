using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    private Vector3 direction;
    public float gravity = -9.8f;
    public float strength = 1f;

    private SpriteRenderer spriteRenderer;
    public Sprite[] flyingSprites;
    private int spriteIndex = 0;
    public float animationSpeed = 0.15f;

    private int health = 1;
    public int maxHealth = 3;

    private GameObject helmetDisplay;
    public bool hasHelmet { get; private set; } = false;

    private float screenLeft, screenRight, screenTop, screenBottom;
    private bool hasLeftScreen = false;

    private void AnimateSprite()
    {
        spriteIndex++;
        if (spriteIndex >= flyingSprites.Length)
            spriteIndex = 0;
        spriteRenderer.sprite = flyingSprites[spriteIndex];
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        helmetDisplay = transform.Find("HelmetDisplay")?.gameObject;
        if (helmetDisplay != null)
            helmetDisplay.SetActive(false);

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
        InvokeRepeating(nameof(AnimateSprite), animationSpeed, animationSpeed);
    }

    private void OnEnable()
    {
        Vector3 position = transform.position;
        position.y = 0f;
        transform.position = position;
        direction = Vector3.zero;
        health = 1;
        hasHelmet = false;
        hasLeftScreen = false;
        if (helmetDisplay != null)
            helmetDisplay.SetActive(false);
    }

    private void Update()
    {
        bool flap =
            (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
            (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);

        if (flap)
            direction = Vector3.up * strength;

        direction.y += gravity * Time.deltaTime;
        transform.position += direction * Time.deltaTime;

        CheckOffScreenAndTriggerDefense();
    }

    private void CheckOffScreenAndTriggerDefense()
    {
        bool isOffScreen = (transform.position.x < screenLeft ||
                            transform.position.x > screenRight ||
                            transform.position.y < screenBottom ||
                            transform.position.y > screenTop);

        if (isOffScreen && !hasLeftScreen)
        {
            hasLeftScreen = true;
            Debug.Log($"[Player] Ball went off-screen at position: {transform.position}");

            if (GameManager.CurrentGameMode == GameManager.GameMode.GameDay)
            {
                var gameDayMgr = FindObjectOfType<GameDayManager>();
                if (gameDayMgr != null)
                    gameDayMgr.StartDefenseRound();
            }
        }
        else if (!isOffScreen)
        {
            hasLeftScreen = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[Player] Collided with {other.gameObject.name} (Tag: {other.tag}) at {other.transform.position}");

        if (other.gameObject.CompareTag("Obstacle"))
        {
            TakeDamage();
        }
        else if (other.gameObject.CompareTag("Scoring"))
        {
            GameManager.IncreaseScore();
        }
        else if (other.gameObject.CompareTag("Collectible"))
        {
            HandleCollectible(other.gameObject);
        }
    }

    private void TakeDamage()
    {
        health--;
        GameManager.OnPlayerDamaged(health);

        if (health == 1)
        {
            hasHelmet = false;
            if (helmetDisplay != null)
                helmetDisplay.SetActive(false);
        }

        if (health <= 0)
        {
            GameManager.GameOver();
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
        if (health > 1)
        {
            hasHelmet = true;
            if (helmetDisplay != null)
                helmetDisplay.SetActive(true);
        }
        GameManager.OnPlayerHealed(health);
    }

    public int GetHealth() => health;
    public int GetMaxHealth() => maxHealth;
}
