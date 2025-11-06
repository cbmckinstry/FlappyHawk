using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IowaManager : MonoBehaviour
{
    public static IowaManager Instance { get; private set; }

    [Header("Scene References")]
    public Player player;
    public TextMeshProUGUI scoreText;
    public GameObject playButton;
    public GameObject gameOver;
    public GameObject readyButton;
    public GameObject menuButton;
    public Image difficultyImage;
    public Sprite easySprite;
    public Sprite normalSprite;
    public Sprite hardSprite;

    [Header("Tuning")]
    [SerializeField] private float pipeSpeed = 5f;
    [SerializeField] private float easySpawnRate = 1.15f;
    [SerializeField] private float normalSpawnRate = 1.00f;
    [SerializeField] private float hardSpawnRate = 0.85f;

    private int score;
    public GameManager.Difficulty CurrentDifficulty => currentDifficulty;
    private GameManager.Difficulty currentDifficulty;

    public static GameManager.Difficulty StartDifficulty = GameManager.Difficulty.Easy;

    public static event Action<float> OnPipeSpeedChanged;
    public static event Action<float> OnSpawnRateChanged;

    public float CurrentPipeSpeed { get; private set; }
    public float CurrentSpawnRate { get; private set; }

    private DateTime roundStartUtc;
    private float roundElapsed;
    private int pipesSpawned;
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
    pipesSpawned = 0;
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
        if (obj is Pipes or Silo or Turbine or Balloon or CycloneBird or CornKernel or Helmet or Football)
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

        CurrentPipeSpeed = pipeSpeed;
        player.gravity = -9.8f;
        CurrentSpawnRate = spawnRate;

        OnPipeSpeedChanged?.Invoke(pipeSpeed);
        OnSpawnRateChanged?.Invoke(spawnRate);

        if (difficultyImage != null)
            difficultyImage.sprite = currentSprite;
    }

    public void RegisterPipe() => pipesSpawned++;
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
