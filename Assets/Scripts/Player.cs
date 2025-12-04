using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class Player : MonoBehaviour
{
    // ===========================
    // MULTIPLAYER FIELDS
    // ===========================
    public enum PlayerID { Player1, Player2 }
    public PlayerID playerID;
    public bool isMultiplayer = false;

    // ===========================

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
            Vector3 topRight = Camera.main.ScreenToWorldPoint(
                new Vector3(Camera.main.pixelWidth, Camera.main.pixelHeight));

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
        direction = Vector3.zero;
        playerHealth = maxPlayerHealth;
        helmetDurability = 0;
        hasHelmet = false;

        if (!isMultiplayer)
        {
            Vector3 pos = transform.position;
            pos.x = 0f;
            pos.y = 0f;
            transform.position = pos;
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f || !PauseManager.GameIsActive)
            return;

        bool flap = false;

        // ===========================================
        // MULTIPLAYER INPUT
        // ===========================================
        if (isMultiplayer)
        {
            if (playerID == PlayerID.Player1)
                flap = Keyboard.current?.wKey.wasPressedThisFrame ?? false;

            if (playerID == PlayerID.Player2)
                flap = Keyboard.current?.upArrowKey.wasPressedThisFrame ?? false;

            // BALL DROP (S / DownArrow)
            bool drop = false;

            if (playerID == PlayerID.Player1)
                drop = Keyboard.current?.sKey.wasPressedThisFrame ?? false;

            if (playerID == PlayerID.Player2)
                drop = Keyboard.current?.downArrowKey.wasPressedThisFrame ?? false;

            if (drop)
                MultiplayerManager.Instance?.HandleFootballDrop(this);
        }
        // ===========================================
        // SINGLE PLAYER INPUT
        // ===========================================
        else
        {
            flap =
                (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
                (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
        }

        // ===========================================
        // APPLY FLAP
        // ===========================================
        if (flap && !isKnockedBack)
        {
            if (transform.position.y <= screenTop - 1.5f)
            {
                AudioManager.Instance?.PlayWingFlap();
                direction = Vector3.up * strength;
            }
        }

        // ===========================================
        // MOVEMENT + BOOSTS
        // ===========================================
        if (isKnockedBack)
        {
            transform.position += knockbackVelocity * Time.deltaTime;
        }
        else
        {
            direction.y += gravity * Time.deltaTime;
            Vector3 movement = direction * Time.deltaTime;
            movement.x += boostVelocityX * Time.deltaTime;
            transform.position += movement;

            if (boostTimeRemaining > 0f)
                boostTimeRemaining -= Time.deltaTime;
            else
                boostVelocityX = 0f;
        }
    }

    // ============================================================
    //  COLLISIONS
    // ============================================================
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[Player] Collided with {other.gameObject.name} ({other.tag})");

        // ========================
        // MULTIPLAYER COLLISION
        // ========================
        if (isMultiplayer)
        {
            var mp = MultiplayerManager.Instance;

            // Ignore special defense ball carrier bird
            if (other.GetComponent<MultiplayerBallCarrierBird>() != null)
                return;

            // Enemy birds
            if (other.CompareTag("Obstacle"))
            {
                bool isCarrier = mp.IsBallCarrier(this);

                if (!mp.InDefenseRound)
                {
                    // OFFENSE
                    if (isCarrier)
                        mp.GameOver();
                    else
                        Destroy(other.gameObject);
                }
                else
                {
                    // DEFENSE → both players destroy birds
                    Destroy(other.gameObject);
                }

                return;
            }

            if (other.CompareTag("Ground"))
            {
                mp.GameOver();
                return;
            }

            if (other.CompareTag("Scoring"))
            {
                mp.OnPlayerEnteredScoring(this);
                return;
            }

            return;
        }


        // ========================
        // SINGLE PLAYER COLLISION
        // ========================
        if (other.CompareTag("Obstacle"))
            TakeDamage();

        else if (other.CompareTag("Ground"))
            DieToGround();

        else if (other.CompareTag("Scoring"))
            GameManager.IncreaseScore();

        else if (other.CompareTag("Collectible"))
            HandleCollectible(other.gameObject);
    }

    // ============================================================
    //  HELMET FOR MULTIPLAYER
    // ============================================================
    public void SetMultiplayerHelmet(bool enabled)
    {
        hasHelmet = enabled;
        if (enabled)
        {
            helmetDurability = int.MaxValue;
            helmetDisplay?.SetActive(true);
        }
        else
        {
            helmetDurability = 0;
            helmetDisplay?.SetActive(false);
        }
    }

    // ============================================================
    //  REMAINING SINGLE PLAYER CODE (unchanged)
    // ============================================================

    private void TakeDamage()
    {
        if (isInvulnerable) return;

        if (helmetDurability > 0)
        {
            helmetDurability--;
            if (helmetDurability == 0)
                helmetDisplay?.SetActive(false);
            ApplyDamageInvulnerability();
        }
        else
        {
            GameManager.GameOver();
        }
    }

    private void DieToGround()
    {
        Debug.Log("[Player] Hit ground");
        GameManager.GameOver();
    }

    private void ApplyDamageInvulnerability()
    {
        if (colorAnimationCoroutine != null)
            StopCoroutine(colorAnimationCoroutine);

        isInvulnerable = true;
        colorAnimationCoroutine = StartCoroutine(AnimateColorGradient(Color.black, INVULNERABILITY_DURATION));
    }

    // ---------------- COLLECTIBLES ----------------
    private void HandleCollectible(GameObject collectible)
    {
        ICollectible col = collectible.GetComponent<ICollectible>();
        if (col != null)
            col.Collect(this);

        Destroy(collectible);
    }

    // ---------------- HEALTH METHODS ----------------
    public void GainHealth(int amount)
    {
        playerHealth = Mathf.Min(playerHealth + amount, maxPlayerHealth);
        ApplyHealthInvulnerability();
    }

    public void SetMaxHealth(int newMaxHealth)
    {
        maxPlayerHealth = newMaxHealth;
        playerHealth = Mathf.Min(playerHealth, maxPlayerHealth);
    }

    public int GetHealth() => playerHealth;
    public int GetMaxHealth() => maxPlayerHealth;

    // ---------------- HELMET METHODS ----------------
    public void GainHelmet(int amount)
    {
        helmetDurability = Mathf.Min(helmetDurability + amount, maxHelmetDurability);

        if (helmetDurability > 0)
        {
            hasHelmet = true;
            helmetDisplay?.SetActive(true);
        }
    }

    public int GetHelmetDurability() => helmetDurability;
    public int GetMaxHelmetDurability() => maxHelmetDurability;

    // ---------------- HORIZONTAL BOOST ----------------
    public void ApplyHorizontalBoost(float distance, float speed)
    {
        boostVelocityX = speed;
        boostTimeRemaining = distance / speed;
        ApplyBoostInvulnerability();
    }

    // ---------------- CORN MAGNET ----------------
    public void ActivateCornMagnet(float duration)
    {
        magnetDurationRemaining += duration;
        magnetTotalDuration = magnetDurationRemaining;

        bool wasActive = isMagnetActive;
        isMagnetActive = true;

        if (!wasActive)
        {
            cornMagnetDisplay?.SetActive(true);
            Spawner s = FindObjectOfType<Spawner>();
            s?.ActivateProbabilityBoost();
        }
        else if (magnetSpriteRenderer != null)
        {
            Color c = magnetSpriteRenderer.color;
            c.a = 1f;
            magnetSpriteRenderer.color = c;
        }
    }

    private void UpdateCornMagnet()
    {
        if (!isMagnetActive) return;

        magnetDurationRemaining -= Time.deltaTime;

        if (magnetDurationRemaining <= 0f)
        {
            isMagnetActive = false;
            cornMagnetDisplay?.SetActive(false);

            Spawner s = FindObjectOfType<Spawner>();
            s?.DeactivateProbabilityBoost();
            return;
        }

        AutoCollectCornKernels();
    }

    private void AutoCollectCornKernels()
    {
        const float xEpsilon = 0.05f;
        CornKernel[] kernels = FindObjectsOfType<CornKernel>();

        foreach (var k in kernels)
        {
            float dx = Mathf.Abs(k.transform.position.x - transform.position.x);
            if (dx <= xEpsilon)
            {
                k.Collect(this);
                Destroy(k.gameObject);
            }
        }
    }

    public bool IsMagnetActive() => isMagnetActive;

    // ---------------- INVULNERABILITY / COLOR EFFECTS ----------------
    private IEnumerator AnimateColorGradient(Color targetColor, float duration)
    {
        float elapsed = 0f;
        float half = duration / 2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / half;

            if (t <= 1f)
                spriteRenderer.color = Color.Lerp(originalColor, targetColor, t);
            else
                spriteRenderer.color = Color.Lerp(targetColor, originalColor, t - 1f);

            yield return null;
        }

        spriteRenderer.color = originalColor;
        isInvulnerable = false;
    }

    private IEnumerator AnimateRainbowCycle(float duration)
    {
        Color[] colors = {
            Color.red,
            new Color(1f,1f,0f),
            Color.green,
            Color.cyan,
            Color.blue,
            new Color(1f,0f,1f)
        };

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = (elapsed / duration) * colors.Length;

            int i = (int)t % colors.Length;
            int next = (i + 1) % colors.Length;

            float lerpT = t - Mathf.Floor(t);

            spriteRenderer.color = Color.Lerp(colors[i], colors[next], lerpT);

            yield return null;
        }

        spriteRenderer.color = originalColor;
        isInvulnerable = false;
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

}
