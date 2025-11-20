using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class Player : MonoBehaviour
{
    private Vector3 direction;
    public float gravity = -9.8f;
    public float strength = 1f;

    private SpriteRenderer spriteRenderer;
    public Sprite[] flyingSprites;
    private int spriteIndex = 0;
    public float animationSpeed = 0.15f;

    private int playerHealth = 1;
    public int maxPlayerHealth = 1;
    private int helmetDurability = 0;
    public int maxHelmetDurability = 3;

    private GameObject helmetDisplay;
    public bool hasHelmet { get; private set; } = false;

    private GameObject cornMagnetDisplay;
    private SpriteRenderer magnetSpriteRenderer;

    private float screenLeft, screenRight, screenTop, screenBottom;
    private bool hasLeftScreen = false;

    private Vector3 knockbackVelocity = Vector3.zero;
    private bool isKnockedBack = false;
    public static float KNOCKBACK_DISTANCE = 1.5f;
    public static float KNOCKBACK_SPEED = 4.5f;
    public static float KNOCKBACK_DURATION = KNOCKBACK_DISTANCE / KNOCKBACK_SPEED;

    private float boostVelocityX = 0f;
    private float boostTimeRemaining = 0f;

    private bool isInvulnerable = false;
    private Color originalColor = Color.white;
    private const float INVULNERABILITY_DURATION = 2.0f;
    private const float COLOR_TRANSITION_TIME = 0.5f;
    private Coroutine colorAnimationCoroutine;

    private float magnetDurationRemaining = 0f;
    private float magnetTotalDuration = 0f;
    private bool isMagnetActive = false;
    private const float MAGNET_FADE_START_TIME = 10f;

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

        cornMagnetDisplay = transform.Find("CornMagnetVisual")?.gameObject;
        if (cornMagnetDisplay != null)
        {
            magnetSpriteRenderer = cornMagnetDisplay.GetComponent<SpriteRenderer>();
            cornMagnetDisplay.SetActive(false);
        }

        if (Camera.main != null)
        {
            Vector3 bottomLeft = Camera.main.ScreenToWorldPoint(Vector3.zero);
            Vector3 topRight = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, Camera.main.pixelHeight));
            screenLeft = bottomLeft.x - 1f;
            screenRight = topRight.x + 1f;
            screenBottom = bottomLeft.y - 1f;
            screenTop = topRight.y + 1f;

            KNOCKBACK_DISTANCE = Mathf.Abs(screenLeft) / 5f;
            KNOCKBACK_DURATION = KNOCKBACK_DISTANCE / KNOCKBACK_SPEED;
            WindBoost.BOOST_DISTANCE = KNOCKBACK_DISTANCE;
        }
    }

    private void Start()
    {
        InvokeRepeating(nameof(AnimateSprite), animationSpeed, animationSpeed);
    }

    private void OnEnable()
    {
        Vector3 position = transform.position;
        position.x = 0f;
        position.y = 0f;
        transform.position = position;
        direction = Vector3.zero;
        
        if (GameManager.CurrentGameMode == GameManager.GameMode.GameDay)
            maxPlayerHealth = 1;
        else
            maxPlayerHealth = 5;
            
        playerHealth = maxPlayerHealth;
        helmetDurability = 0;
        hasHelmet = false;
        hasLeftScreen = false;
        if (helmetDisplay != null)
            helmetDisplay.SetActive(false);

        isMagnetActive = false;
        magnetDurationRemaining = 0f;
        if (cornMagnetDisplay != null)
            cornMagnetDisplay.SetActive(false);
    }

    private void Update()
    {
        bool flap =
            (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
            (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);

        if (flap && !isKnockedBack)
        {
            if (transform.position.y <= screenTop-1.5f)
            {
                AudioManager.Instance?.PlayWingFlap();
                direction = Vector3.up * strength;
            }
        }

        if (isKnockedBack)
        {
            transform.position += knockbackVelocity * Time.deltaTime;
        }
        else
        {
            direction.y += gravity * Time.deltaTime;
            Vector3 movementThisFrame = direction * Time.deltaTime;
            movementThisFrame.x += boostVelocityX * Time.deltaTime;
            transform.position += movementThisFrame;
            
            if (boostTimeRemaining > 0f)
                boostTimeRemaining -= Time.deltaTime;
            else
                boostVelocityX = 0f;
        }

        UpdateCornMagnet();
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
        if (other.gameObject.CompareTag("Ground"))
        {
            DieToGround();
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
        if (isInvulnerable)
            return;

        if (helmetDurability > 0)
        {
            helmetDurability--;
            GameManager.OnPlayerDamaged(helmetDurability);
            
            if (helmetDurability == 0)
            {
                hasHelmet = false;
                if (helmetDisplay != null)
                    helmetDisplay.SetActive(false);
            }
            ApplyDamageInvulnerability();
        }
        else
        {
            if (GameManager.CurrentGameMode == GameManager.GameMode.GameDay)
            {
                GameManager.GameOver();
                AudioManager.Instance?.PlayDie();
                return;
            }

            if (GameManager.CurrentGameMode == GameManager.GameMode.Iowa)
            {
                ApplyKnockback();
            }

            playerHealth--;
            GameManager.OnPlayerDamaged(helmetDurability);

            if (playerHealth <= 0)
            {
                GameManager.GameOver();
                AudioManager.Instance?.PlayDie();
            }
            else
            {
                ApplyDamageInvulnerability();
            }
        }
    }

    private void ApplyKnockback()
    {
        isKnockedBack = true;
        knockbackVelocity = Vector3.left * KNOCKBACK_SPEED;
        Invoke(nameof(EndKnockback), KNOCKBACK_DURATION);
    }

    private void EndKnockback()
    {
        isKnockedBack = false;
        knockbackVelocity = Vector3.zero;
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
        playerHealth = Mathf.Min(playerHealth + amount, maxPlayerHealth);
        GameManager.OnPlayerHealed(playerHealth);
        ApplyHealthInvulnerability();
    }

    public void GainHelmet(int amount)
    {
        helmetDurability = Mathf.Min(helmetDurability + amount, maxHelmetDurability);
        if (helmetDurability > 0)
        {
            hasHelmet = true;
            if (helmetDisplay != null)
                helmetDisplay.SetActive(true);
        }
        GameManager.OnPlayerHealed(helmetDurability);
    }


private void DieToGround()
{
    Debug.Log("[Player] Ground hit — instant death.");
    AudioManager.Instance?.PlayDie();
    GameManager.GameOver();  // bypass helmet/health entirely
}

    public void ApplyHorizontalBoost(float distance, float speed)
    {
        boostVelocityX = speed;
        boostTimeRemaining += distance / speed;
        ApplyBoostInvulnerability();
    }

    private void ApplyDamageInvulnerability()
    {
        if (colorAnimationCoroutine != null)
            StopCoroutine(colorAnimationCoroutine);
        
        isInvulnerable = true;
        colorAnimationCoroutine = StartCoroutine(AnimateColorGradient(Color.black, INVULNERABILITY_DURATION));
    }

    private void ApplyBoostInvulnerability()
    {
        if (colorAnimationCoroutine != null)
            StopCoroutine(colorAnimationCoroutine);
        
        isInvulnerable = true;
        colorAnimationCoroutine = StartCoroutine(AnimateRainbowCycle(INVULNERABILITY_DURATION));
    }

    private void ApplyHealthInvulnerability()
    {
        if (colorAnimationCoroutine != null)
            StopCoroutine(colorAnimationCoroutine);
        
        isInvulnerable = true;
        colorAnimationCoroutine = StartCoroutine(AnimateRainbowCycle(INVULNERABILITY_DURATION));
    }

    private IEnumerator AnimateColorGradient(Color targetColor, float duration)
    {
        float elapsed = 0f;
        float halfDuration = duration / 2f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            
            if (t <= 1f)
            {
                spriteRenderer.color = Color.Lerp(originalColor, targetColor, t);
            }
            else
            {
                float returnT = (t - 1f);
                spriteRenderer.color = Color.Lerp(targetColor, originalColor, returnT);
            }
            
            yield return null;
        }
        
        spriteRenderer.color = originalColor;
        isInvulnerable = false;
    }

    private IEnumerator AnimateRainbowCycle(float duration)
    {
        Color[] rainbowColors = new Color[]
        {
            Color.red,
            new Color(1f, 1f, 0f),
            Color.green,
            Color.cyan,
            Color.blue,
            new Color(1f, 0f, 1f)
        };

        float elapsed = 0f;
        int colorIndex = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float cycleProgress = (elapsed / duration) * rainbowColors.Length;
            
            colorIndex = (int)cycleProgress % rainbowColors.Length;
            int nextColorIndex = (colorIndex + 1) % rainbowColors.Length;
            
            float colorLerpT = cycleProgress - (int)cycleProgress;
            spriteRenderer.color = Color.Lerp(rainbowColors[colorIndex], rainbowColors[nextColorIndex], colorLerpT);
            
            yield return null;
        }

        spriteRenderer.color = originalColor;
        isInvulnerable = false;
    }

    public void ActivateCornMagnet(float duration)
    {
        bool wasAlreadyActive = isMagnetActive;
        
        magnetDurationRemaining += duration;
        magnetTotalDuration = magnetDurationRemaining;
        isMagnetActive = true;

        if (!wasAlreadyActive)
        {
            CreateMagnetVisual();
            Spawner spawner = FindObjectOfType<Spawner>();
            if (spawner != null)
                spawner.ActivateProbabilityBoost();
        }
        else if (magnetSpriteRenderer != null)
        {
            Color magnetColor = magnetSpriteRenderer.color;
            magnetColor.a = 1f;
            magnetSpriteRenderer.color = magnetColor;
        }
    }

    private void CreateMagnetVisual()
    {
        if (cornMagnetDisplay == null)
            return;

        cornMagnetDisplay.SetActive(true);
        
        if (magnetSpriteRenderer != null)
        {
            Color magnetColor = magnetSpriteRenderer.color;
            magnetColor.a = 1f;
            magnetSpriteRenderer.color = magnetColor;
        }
    }

    private void UpdateCornMagnet()
    {
        if (!isMagnetActive)
            return;

        magnetDurationRemaining -= Time.deltaTime;

        if (magnetDurationRemaining <= 0f)
        {
            isMagnetActive = false;
            if (cornMagnetDisplay != null)
                cornMagnetDisplay.SetActive(false);

            Spawner spawner = FindObjectOfType<Spawner>();
            if (spawner != null)
                spawner.DeactivateProbabilityBoost();
        }
        else
        {
            float timeUntilFade = magnetDurationRemaining - MAGNET_FADE_START_TIME;
            
            if (cornMagnetDisplay != null && magnetSpriteRenderer != null)
            {
                if (timeUntilFade <= 0f)
                {
                    float fadeProgress = (MAGNET_FADE_START_TIME - magnetDurationRemaining) / MAGNET_FADE_START_TIME;
                    Color magnetColor = magnetSpriteRenderer.color;
                    magnetColor.a = Mathf.Lerp(1f, 0f, fadeProgress);
                    magnetSpriteRenderer.color = magnetColor;
                }
                else
                {
                    Color magnetColor = magnetSpriteRenderer.color;
                    magnetColor.a = 1f;
                    magnetSpriteRenderer.color = magnetColor;
                }
            }

            AutoCollectCornKernels();
        }
    }

    private void AutoCollectCornKernels()
{
    // How close in X we consider "the same x coordinate"
    const float xEpsilon = 0.05f;

    CornKernel[] allCornKernels = FindObjectsOfType<CornKernel>();

    foreach (CornKernel kernel in allCornKernels)
    {
        float xDiff = Mathf.Abs(kernel.transform.position.x - transform.position.x);

        // "Same x" within a small tolerance, ignore Y so any kernel that passes that x is grabbed
        if (xDiff <= xEpsilon)
        {
            kernel.Collect(this);
            Destroy(kernel.gameObject);
        }
    }
}

    public bool IsMagnetActive() => isMagnetActive;

    public int GetHealth() => playerHealth;
    public int GetMaxHealth() => maxPlayerHealth;
    public int GetHelmetDurability() => helmetDurability;
    public int GetMaxHelmetDurability() => maxHelmetDurability;
}
