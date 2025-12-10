using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;

public class IowaManager : MonoBehaviour
{
    public static IowaManager Instance { get; private set; }

    [Header("Scene References")]
    public Player player;
    public GameObject playButton;
    public GameObject gameOver;
    public GameObject readyButton;
    public TMP_InputField playerNameInput;

    // UI
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI playerHealthText;

    [Header("Game Over UI")]
    [SerializeField] private TextMeshProUGUI goCurrentScoreText;
    [SerializeField] private TextMeshProUGUI goHighScoreText;
    [SerializeField] private TextMeshProUGUI goModeDifficultyText;

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

        // Show these on load
        readyButton?.SetActive(true);
        playButton?.SetActive(true);
        playerNameInput?.gameObject.SetActive(true);

        // Make sure UI labels are shown normally
        scoreText?.gameObject.SetActive(true);
        playerHealthText?.gameObject.SetActive(true);

        // Hide player object at scene load
        if (player != null)
            player.gameObject.SetActive(false);

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
            roundElapsed += Time.unscaledDeltaTime;

            bool jumpPressed =
                (Keyboard.current?.spaceKey.wasPressedThisFrame ?? false) ||
                (Mouse.current?.leftButton.wasPressedThisFrame ?? false) ||
                (Gamepad.current?.buttonSouth.wasPressedThisFrame ?? false);

            if (jumpPressed) jumps++;
        }
    }

    public void Play()
    {
        PauseManager.GameIsActive = true;

        score = 0;
        scoreText.text = "0";
        obstaclesSpawned = 0;
        jumps = 0;
        roundElapsed = 0f;

        readyButton?.SetActive(false);
        playButton?.SetActive(false);
        playerNameInput?.gameObject.SetActive(false);
        gameOver?.SetActive(false);

        scoreText?.gameObject.SetActive(true);
        playerHealthText?.gameObject.SetActive(true);


        Time.timeScale = 1f;

        ApplyDifficulty();

        // Re-enable player object when gameplay starts
        if (player != null)
            player.gameObject.SetActive(true);

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
            if (obj is Obstacle or Silo or Turbine or Balloon or CycloneBird
                or CornKernel or Helmet or CornMagnet or WindBoost
                or Football or GoalPost or BallCarrierBird)
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
        PauseManager.GameIsActive = false;

        if (!CustomSpawnSettings.IsCustomIowa)
        {
            LogIowaRun();
        }

        if (player != null)
            player.gameObject.SetActive(false);

        if (goCurrentScoreText != null)
            goCurrentScoreText.text = score.ToString();

        if (goModeDifficultyText != null)
            goModeDifficultyText.text = $"Iowa — {CurrentDifficulty}";

        int highScore = LoadIowaHighScore(CurrentDifficulty.ToString());
        if (goHighScoreText != null)
            goHighScoreText.text = highScore.ToString();

        // Hide player when Game Over happens
        if (player != null)
            player.gameObject.SetActive(false);

        gameOver.SetActive(true);
        playButton.SetActive(true);
        readyButton?.SetActive(false);
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
                maxHealth = 4;
                break;
            case GameManager.Difficulty.Hard:
                spawnRate = hardSpawnRate;
                maxHealth = 3;
                break;
            default:
                spawnRate = easySpawnRate;
                maxHealth = 5;
                break;
        }

        CurrentScrollSpeed = scrollSpeed;
        player.gravity = -9.8f;
        CurrentSpawnRate = spawnRate;
        player.SetMaxHealth(maxHealth);

        OnScrollSpeedChanged?.Invoke(scrollSpeed);
        OnSpawnRateChanged?.Invoke(spawnRate);
    }

    public void RegisterObstacle() => obstaclesSpawned++;
    public void RegisterJump() => jumps++;

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
        // Iowa version of run data
        RunLogData data = new RunLogData
        {
            playerName = GetFinalizedPlayerName(),

            gameMode = "Iowa",
            difficulty = currentDifficulty.ToString(),

            score = score,
            playerScore = 0,
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

    private int LoadIowaHighScore(string difficultyFilter)
    {
        string folder = RunDataLogger.GetLogFolder();
        string filePath = Path.Combine(folder, "game_runs.csv");

        if (!File.Exists(filePath))
            return 0;

        try
        {
            var lines = File.ReadAllLines(filePath).Skip(1); // skip header
            int best = 0;

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split(',');
                if (parts.Length < 5)
                    continue;

                string mode = parts[2].Trim().Trim('"');    // game_mode
                string diff = parts[3].Trim().Trim('"');    // difficulty

                if (mode != "Iowa") continue;
                if (diff != difficultyFilter) continue;

                if (!int.TryParse(parts[4], out int scoreValue))
                    continue;

                best = Mathf.Max(best, scoreValue);
            }

            return best;
        }
        catch
        {
            return 0;
        }
    }
}
