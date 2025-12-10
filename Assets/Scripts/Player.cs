using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;

public class Player : MonoBehaviour
{
    // ===========================
    // MULTIPLAYER FIELDS
    // ===========================
    public enum PlayerID { Player1, Player2 }
    public PlayerID playerID;
    public bool isMultiplayer = false;   // will be overridden automatically by scene

    // ===========================
    // SHARED FIELDS
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

    // ============================================================
    //  ANIMATION
    // ============================================================
    private void AnimateSprite()
    {
        spriteIndex++;
        if (spriteIndex >= flyingSprites.Length)
            spriteIndex = 0;
        spriteRenderer.sprite = flyingSprites[spriteIndex];
    }

    // ============================================================
    //  AWAKE
    // ============================================================
    private void Awake()
    {
        // AUTO-DETECT MULTIPLAYER BY SCENE
        string sceneName = SceneManager.GetActiveScene().name;
        isMultiplayer = (sceneName == "MultiplayerScene");

        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

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

    // ============================================================
    //  ONENABLE
    // ============================================================
    private void OnEnable()
    {
        direction = Vector3.zero;
        helmetDurability = 0;
        hasHelmet = false;
        hasLeftScreen = false;
        isInvulnerable = false;
        isKnockedBack = false;
        boostVelocityX = 0f;
        boostTimeRemaining = 0f;

        if (helmetDisplay != null)
            helmetDisplay.SetActive(false);

        if (cornMagnetDisplay != null)
        {
            cornMagnetDisplay.SetActive(false);
            if (magnetSpriteRenderer != null)
            {
                var c = magnetSpriteRenderer.color;
                c.a = 1f;
                magnetSpriteRenderer.color = c;
            }
        }

        // SINGLE-PLAYER SETUP
        if (!isMultiplayer)
        {
            // Reset position to center
            Vector3 position = transform.position;
            position.x = 0f;
            position.y = 0f;
            transform.position = position;

            // Health rules from your original single-player
            if (GameManager.CurrentGameMode == GameManager.GameMode.GameDay)
                maxPlayerHealth = 1;
            else
                maxPlayerHealth = 5;

            playerHealth = maxPlayerHealth;
            magnetDurationRemaining = 0f;
            magnetTotalDuration = 0f;
            isMagnetActive = false;
        }
        else
        {
            // MULTIPLAYER: health isn't really used; MP logic handles deaths via MultiplayerManager
            // Still keep something sane
            maxPlayerHealth = 1;
            playerHealth = 1;
        }
    }

    // ============================================================
    //  UPDATE
    // ============================================================
    private void Update()
    {
        if (Time.timeScale == 0f || !PauseManager.GameIsActive)
            return;

        bool flap = false;
        bool drop = false;

        // ===========================================
        // MULTIPLAYER INPUT
        // ===========================================
        if (isMultiplayer)
        {
            var input = ControllerInputManager.Instance;

            // Controller inputs ONLY
            flap = input.GetFlap(playerID);
            drop = input.GetDrop(playerID);

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

        // SINGLE-PLAYER-ONLY LOGIC: magnet + off-screen defense trigger
        if (!isMultiplayer)
        {
            UpdateCornMagnet();
            CheckOffScreenAndTriggerDefense();
        }
    }

    // ============================================================
    //  SINGLE-PLAYER OFF-SCREEN → DEFENSE (GAMEDAY)
    // ============================================================
    private void CheckOffScreenAndTriggerDefense()
    {
        bool isOffScreen =
            (transform.position.x < screenLeft ||
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

    // ============================================================
    //  COLLISIONS
    // ============================================================
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[Player] Collided with {other.gameObject.name} ({other.tag})");

        // ========================
        // MULTIPLAYER COLLISIONS
        // ========================
        if (isMultiplayer)
        {
            var mp = MultiplayerManager.Instance;

            // Ignore the special defense ball carrier bird – that script resolves defense
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
                    {
                        mp.GameOver();
                        AudioManager.Instance?.PlayGrunt();
                    }

                    else
                        Destroy(other.gameObject);
                }
                else
                {
                    // DEFENSE – both players can destroy birds
                    Destroy(other.gameObject);
                }

                return;
            }

            // Ground = instant game over for team
            if (other.CompareTag("Ground"))
            {
                mp.GameOver();
                return;
            }

            // Scoring (safety guard – normal scoring already done via football/goalpost)
            if (other.CompareTag("Scoring"))
            {
                mp.OnPlayerEnteredScoring(this);
                return;
            }

            return;
        }

        // ========================
        // SINGLE-PLAYER COLLISIONS
        // ========================
        if (other.gameObject.CompareTag("Obstacle"))
        {
            TakeDamage();
        }
        else if (other.gameObject.CompareTag("Ground"))
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

    // ============================================================
    //  MULTIPLAYER HELMET
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
    //  SINGLE-PLAYER DAMAGE / HEALTH
    // ============================================================
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
            // GameDay: straight up game over
            if (GameManager.CurrentGameMode == GameManager.GameMode.GameDay)
            {
                GameManager.GameOver();
                AudioManager.Instance?.PlayGrunt();
                return;
            }

            // Iowa: knockback instead of instant death
            if (GameManager.CurrentGameMode == GameManager.GameMode.Iowa)
            {
                ApplyKnockback();
            }

            playerHealth--;
            GameManager.OnPlayerDamaged(helmetDurability);

            if (playerHealth <= 0)
            {
                GameManager.GameOver();
                AudioManager.Instance?.PlayGameOver();
            }
            else
            {
                ApplyDamageInvulnerability();
            }
        }
    }

    private void ApplyKnockback()
    {
        AudioManager.Instance?.PlayTackle();
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

    public void SetMaxHealth(int newMaxHealth)
    {
        maxPlayerHealth = newMaxHealth;
        playerHealth = Mathf.Min(playerHealth, maxPlayerHealth);
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
        AudioManager.Instance?.PlaySplat();
        GameManager.GameOver();
    }

    // ============================================================
    //  BOOSTS
    // ============================================================
    public void ApplyHorizontalBoost(float distance, float speed)
    {
        boostVelocityX = speed;
        boostTimeRemaining += distance / speed;
        ApplyBoostInvulnerability();
    }

    // ============================================================
    //  INVULNERABILITY / COLOR EFFECTS
    // ============================================================
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

    // ============================================================
    //  CORN MAGNET (SINGLE-PLAYER)
    // ============================================================
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
            // Original “any kernel at same X” auto-collect behavior
            AutoCollectCornKernels();
        }
    }

    private void AutoCollectCornKernels()
    {
        const float xEpsilon = 0.05f;

        CornKernel[] allCornKernels = FindObjectsOfType<CornKernel>();

        foreach (CornKernel kernel in allCornKernels)
        {
            float xDiff = Mathf.Abs(kernel.transform.position.x - transform.position.x);
            if (xDiff <= xEpsilon)
            {
                kernel.Collect(this);
                Destroy(kernel.gameObject);
            }
        }
    }

    public bool IsMagnetActive() => isMagnetActive;

    // Public getters (used elsewhere)
    public int GetHealth() => playerHealth;
    public int GetMaxHealth() => maxPlayerHealth;
    public int GetHelmetDurability() => helmetDurability;
    public int GetMaxHelmetDurability() => maxHelmetDurability;
}
