using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class IowaManager : MonoBehaviour
{
    public static IowaManager Instance { get; private set; }

    [Header("Scene References")]
    public Player player;
    public GameObject playButton;
    public GameObject gameOver;
    public GameObject readyButton;
    public GameObject menuButton;
    public Image difficultyImage;
    public Sprite easySprite;
    public Sprite normalSprite;
    public Sprite hardSprite;
    public TMP_InputField playerNameInput;

    // UI
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI helmetDurabilityText;
    [SerializeField] private TextMeshProUGUI playerHealthText;

    [Header("Tuning")]
    [SerializeField] private float scrollSpeed = 5f;
    [SerializeField] private float easySpawnRate = 1.15f;
    [SerializeField] private float normalSpawnRate = 1.00f;
    [SerializeField] private float hardSpawnRate = 0.85f;

    private int score;
    public GameManager.Difficulty CurrentDifficulty => currentDifficulty;
    private GameManager.Difficulty currentDifficulty;

    public static GameManager.Difficulty StartDifficulty = GameManager.Difficulty.Easy;

    public static event Action<float> OnScrollSpeedChanged;
    public static event Action<float> OnSpawnRateChanged;

    public float CurrentScrollSpeed { get; private set; }
    public float CurrentSpawnRate { get; private set; }

    private float roundElapsed;
    private int obstaclesSpawned;
    private int jumps;

    private void Awake()
    {
        Instance = this;
        Application.targetFrameRate = 60;

        gameOver.SetActive(false);
        difficultyImage?.gameObject.SetActive(false);

        // Show these on load
        readyButton?.SetActive(true);         
        playButton?.SetActive(true);
        menuButton?.SetActive(true);
        playerNameInput?.gameObject.SetActive(true);

        // Using TextMeshPro fields you already have:
        scoreText?.gameObject.SetActive(true);
        helmetDurabilityText?.gameObject.SetActive(true);
        playerHealthText?.gameObject.SetActive(true);

        Pause();

        currentDifficulty = StartDifficulty;
        ApplyDifficulty();
    }

    private void Start()
    {
        SelectPlayButton();
    }

    private void Update()
    {
        if (player != null && player.enabled && Time.timeScale > 0f)
        {
            // Timer
            roundElapsed += Time.unscaledDeltaTime;

            // Jump counter
            bool jumpPressed =
                (Keyboard.current?.spaceKey.wasPressedThisFrame ?? false) ||
                (Mouse.current?.leftButton.wasPressedThisFrame ?? false) ||
                (Gamepad.current?.buttonSouth.wasPressedThisFrame ?? false);

            if (jumpPressed) jumps++;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
            QuitGame();
    }

    public void Play()
    {
        score = 0;
        scoreText.text = "0";
        obstaclesSpawned = 0;
        jumps = 0;
        roundElapsed = 0f;

        readyButton?.SetActive(false);
        playButton?.SetActive(false);
        menuButton?.SetActive(false);
        playerNameInput?.gameObject.SetActive(false);
        difficultyImage?.gameObject.SetActive(false);
        gameOver?.SetActive(false);

        scoreText?.gameObject.SetActive(true);
        helmetDurabilityText?.gameObject.SetActive(true);
        playerHealthText?.gameObject.SetActive(true);

        if (playerNameInput != null)
            playerNameInput.text = "";

        Time.timeScale = 1f;
        
        ApplyDifficulty();
        
        player.enabled = true;
        
        int maxHealth = currentDifficulty switch
        {
            GameManager.Difficulty.Normal => 4,
            GameManager.Difficulty.Hard => 3,
            _ => 5
        };
        player.SetMaxHealth(maxHealth);

        Transform cornMagnetVisual = player.transform.Find("CornMagnetVisual");
        if (cornMagnetVisual != null)
            cornMagnetVisual.gameObject.SetActive(false);

        UpdateAllDisplays();

        // wipe old run's spawned objects
        foreach (var obj in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            if (obj is Obstacle or Silo or Turbine or Balloon or CycloneBird or CornKernel or Helmet or CornMagnet or WindBoost or Football or GoalPost or BallCarrierBird)
                Destroy(obj.gameObject);

        // reset Game Day if present
        var gdm = FindFirstObjectByType<GameDayManager>();
        if (gdm != null)
        {
            gdm.ResetScores();
            gdm.OnPlayerDeathReset();
        }



        FindFirstObjectByType<Spawner>()?.ResetSpawner();
    }

    public void GameOver()
    {
        LogIowaRun();

        gameOver.SetActive(true);
        playButton.SetActive(true);
        menuButton.SetActive(true);

        // Hide these on GameOver screen
        readyButton?.SetActive(false);
        difficultyImage?.gameObject.SetActive(false);
        playerNameInput?.gameObject.SetActive(false);

        Pause();
        SelectPlayButton();
    }

    private void SelectPlayButton()
    {
        Button button = playButton?.GetComponent<Button>();
        if (button != null)
            EventSystem.current?.SetSelectedGameObject(button.gameObject);
    }

    public bool IsGameActive()
    {
        return Time.timeScale > 0f && player != null && player.enabled;
    }

    public void IncreaseScore(int amount = 1)
    {
        score += amount;
        scoreText.text = score.ToString();
    }

    // bridge stub (Iowa never uses opponent score)
    public void IncreaseOpponentScore(int amount = 1) { }

    public void Pause()
    {
        Time.timeScale = 0f;
        player.enabled = false;
    }

    private void ApplyDifficulty()
    {
        float spawnRate;
        Sprite currentSprite;
        int maxHealth;

        switch (currentDifficulty)
        {
            case GameManager.Difficulty.Normal:
                spawnRate = normalSpawnRate;
                currentSprite = normalSprite;
                maxHealth = 4;
                break;
            case GameManager.Difficulty.Hard:
                spawnRate = hardSpawnRate;
                currentSprite = hardSprite;
                maxHealth = 3;
                break;
            default:
                spawnRate = easySpawnRate;
                currentSprite = easySprite;
                maxHealth = 5;
                break;
        }

        CurrentScrollSpeed = scrollSpeed;
        player.gravity = -9.8f;
        CurrentSpawnRate = spawnRate;
        player.SetMaxHealth(maxHealth);

        OnScrollSpeedChanged?.Invoke(scrollSpeed);
        OnSpawnRateChanged?.Invoke(spawnRate);

        if (difficultyImage != null)
            difficultyImage.sprite = currentSprite;
    }

    public void RegisterObstacle() => obstaclesSpawned++;
    public void RegisterJump() => jumps++;

    private void UpdateHelmetDurabilityDisplay()
    {
        if (player == null)
            player = FindObjectOfType<Player>();

        if (helmetDurabilityText == null)
            helmetDurabilityText = GameObject.Find("HelmetNumber")?.GetComponent<TextMeshProUGUI>();

        if (player != null && helmetDurabilityText != null)
            helmetDurabilityText.text = player.GetHelmetDurability().ToString();
    }

    private void UpdatePlayerHealthDisplay()
    {
        if (player == null)
            player = FindObjectOfType<Player>();

        if (playerHealthText == null)
            playerHealthText = GameObject.Find("HealthNumber")?.GetComponent<TextMeshProUGUI>();

        if (player != null && playerHealthText != null)
            playerHealthText.text = player.GetHealth().ToString();
    }

    private void UpdateAllDisplays()
    {
        UpdateHelmetDurabilityDisplay();
        UpdatePlayerHealthDisplay();
    }

    public void OnPlayerDamaged(int helmetDurability)
    {
        UpdateAllDisplays();
    }

    public void OnPlayerHealed(int helmetDurability)
    {
        UpdateAllDisplays();
    }

    public void ReturnToMainMenu()
    {
        AudioManager.Instance?.PlayClickSound();
        Time.timeScale = 1f;
        SceneManager.LoadScene("MenuScreen");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ----------------------- LOGGING ----------------------
    private string GetFinalizedPlayerName()
    {
        if (playerNameInput == null) return "Unknown";

        playerNameInput.DeactivateInputField();
        playerNameInput.ForceLabelUpdate();

        string name = playerNameInput.text.Trim();
        return string.IsNullOrEmpty(name) ? "Unknown" : name;
    }

    private void LogIowaRun()
    {
        RunLogData data = new RunLogData
        {
            playerName = GetFinalizedPlayerName(),

            gameMode = "Iowa",
            difficulty = currentDifficulty.ToString(),

            score = score,
            playerScore = score,
            enemyScore = 0,

            roundSeconds = roundElapsed,

            obstaclesSpawned = obstaclesSpawned,
            jumps = jumps,
            helmetsCollected = 0,

            offenseDrives = 0,
            defenseRoundsWon = 0,
            defenseRoundsFailed = 0
        };

        RunDataLogger.AppendRun(data);
    }
}
