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

    // UI (internal labels)
    [SerializeField] private TextMeshProUGUI scoreText;

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

    private DateTime roundStartUtc;
    private float roundElapsed;
    private int obstaclesSpawned;
    private int jumps;

    private void Awake()
    {
        Instance = this;
        Application.targetFrameRate = 60;
        gameOver.SetActive(false);
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
    roundStartUtc = DateTime.UtcNow;

    playButton.SetActive(false);
    gameOver.SetActive(false);
    menuButton.SetActive(false);
    readyButton?.SetActive(false);
    difficultyImage?.gameObject.SetActive(false);

    Time.timeScale = 1f;
    player.enabled = true;

    // wipe old run’s spawned objects
    foreach (var obj in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        if (obj is Obstacle or Silo or Turbine or Balloon or CycloneBird or CornKernel or Helmet or Football or GoalPost or BallCarrierBird)
            Destroy(obj.gameObject);

        // >>> NEW: reset Game Day
        var gdm = FindFirstObjectByType<GameDayManager>();
    if (gdm != null)
    {
        gdm.ResetScores();         // 0–out the UI right away
        gdm.OnPlayerDeathReset();  // also clears defense flags and resets spawner
    }
    // <<< END NEW

    // your existing reset (safe even if also called by OnPlayerDeathReset)
    FindFirstObjectByType<Spawner>()?.ResetSpawner();

    ApplyDifficulty();
}


    public void GameOver()
    {
        gameOver.SetActive(true);
        playButton.SetActive(true);
        readyButton?.SetActive(false);
        difficultyImage?.gameObject.SetActive(true);
        menuButton?.SetActive(true);
        Pause();
        
        SelectPlayButton();
    }

    private void SelectPlayButton()
    {
        Button button = playButton?.GetComponent<Button>();
        if (button != null)
        {
            EventSystem.current?.SetSelectedGameObject(button.gameObject);
        }
    }

    public bool IsGameActive()
    {
        // Game is active when:
        // - Time is moving (not paused)
        // - Player is enabled (not dead / not on title screen)
        return Time.timeScale > 0f && player != null && player.enabled;
    }

    public void IncreaseScore(int amount = 1)
    {
        score += amount;
        scoreText.text = score.ToString();
    }

    // Compatibility stub for GameManager bridge
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

        switch (currentDifficulty)
        {
            case GameManager.Difficulty.Normal:
                spawnRate = normalSpawnRate;
                currentSprite = normalSprite;
                break;
            case GameManager.Difficulty.Hard:
                spawnRate = hardSpawnRate;
                currentSprite = hardSprite;
                break;
            default:
                spawnRate = easySpawnRate;
                currentSprite = easySprite;
                break;
        }

        CurrentScrollSpeed = scrollSpeed;
        player.gravity = -9.8f;
        CurrentSpawnRate = spawnRate;

        OnScrollSpeedChanged?.Invoke(scrollSpeed);
        OnSpawnRateChanged?.Invoke(spawnRate);

        if (difficultyImage != null)
            difficultyImage.sprite = currentSprite;
    }

    public void RegisterObstacle() => obstaclesSpawned++;
    public void RegisterJump() => jumps++;



public void ReturnToMainMenu()
{
    AudioManager.Instance?.PlayClickSound();
    Time.timeScale = 1f; // unpause in case it�s paused
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
}
