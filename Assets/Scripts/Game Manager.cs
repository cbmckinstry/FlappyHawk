using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.IO;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public enum Difficulty { Easy = 0, Normal = 1, Hard = 2 }
public enum GameMode { Normal = 0, GameDay = 1 }
public enum GameDayDifficulty { College = 0, Pro = 1 }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public Player player;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI opponentScoreText; // New: opponent score for Game Day Mode
    public GameObject playButton;
    public GameObject gameOver;
    public GameObject readyButton;
    public GameObject quitButton;
    public GameObject dropdownMenu;

    [Header("Difficulty UI")]
    public Image difficultyImage;
    public Sprite easySprite;
    public Sprite normalSprite;
    public Sprite hardSprite;

    [Header("Game Day Mode")]
    private GameMode currentGameMode = GameMode.Normal;
    private GameDayDifficulty gameDayDifficulty = GameDayDifficulty.College;
    public GameMode CurrentGameMode => currentGameMode;
    public GameDayDifficulty CurrentGameDayDifficulty => gameDayDifficulty;

    private int score;
    private int opponentScore; // New: opponent score
    private Difficulty currentDifficulty = Difficulty.Easy;
    public Difficulty CurrentDifficulty => currentDifficulty;

    public static event Action<float> OnPipeSpeedChanged;
    public float CurrentPipeSpeed { get; private set; } 

    public static event Action<Difficulty> OnDifficultyChanged;

    private float pipeSpeed = 5f;
    private float easySpawnRate = 1.15f;
    private float normalSpawnRate = 1f;
    private float hardSpawnRate = 0.85f;
    private float gameDaySpawnRate = 1.2f;

    public static event Action<float> OnSpawnRateChanged;
    
    private float easyGravity = -9.8f;
    private float normalGravity = -9.8f;
    private float hardGravity = -9.8f;

    private DateTime roundStartUtc;
    private float roundElapsed; 
    private int pipesSpawnedThisRound;
    private int jumpsThisRound;

    public void IncreaseScore(int amount = 1)
    {
        score += amount;
        scoreText.text = score.ToString();
    }

    public void IncreaseOpponentScore(int amount = 1)
    {
        if (currentGameMode == GameMode.GameDay)
        {
            opponentScore += amount;
            if (opponentScoreText != null)
                opponentScoreText.text = opponentScore.ToString();
        }
    }

    public void Awake() { 
        Instance = this;
        Application.targetFrameRate = 60; 
        gameOver.SetActive(false);
        Pause(); 
        ApplyDifficulty();
        Debug.Log($"[GameManager] Initialized with difficulty: {currentDifficulty}");
        
        if (dropdownMenu != null)
        {
            Dropdown dropdown = dropdownMenu.GetComponent<Dropdown>();
            if (dropdown != null)
            {
                dropdown.onValueChanged.AddListener(OnDifficultyDropdownChanged);
                Debug.Log("[GameManager] Dropdown hooked up!");
            }
            else
            {
                Debug.Log("[GameManager] dropdownMenu found but has no Dropdown component!");
            }
        }
        else
        {
            Debug.Log("[GameManager] dropdownMenu is null!");
        }
    }
    
    private void OnDifficultyDropdownChanged(int value)
    {
        Debug.Log($"[GameManager] Dropdown value changed to: {value}");
        
        if (value >= 0 && value < 3)
        {
            currentGameMode = GameMode.Normal;
            currentDifficulty = (Difficulty)value;
            Debug.Log($"[GameManager] Normal mode - Difficulty set to: {currentDifficulty}");
        }
        else if (value == 3)
        {
            currentGameMode = GameMode.GameDay;
            gameDayDifficulty = GameDayDifficulty.College;
            Debug.Log($"[GameManager] Game Day mode - College difficulty set");
        }
        else if (value == 4)
        {
            currentGameMode = GameMode.GameDay;
            gameDayDifficulty = GameDayDifficulty.Pro;
            Debug.Log($"[GameManager] Game Day mode - Pro difficulty set");
        }
    }

    private void Update()
{
    if (player != null && player.enabled && Time.timeScale > 0f)
    {
        roundElapsed += Time.unscaledDeltaTime;

        bool jumpPressed =
            (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
            (Mouse.current    != null && Mouse.current.leftButton.wasPressedThisFrame)  ||
            (Gamepad.current  != null && Gamepad.current.buttonSouth.wasPressedThisFrame);

        if (jumpPressed)
            jumpsThisRound++;
    }

    if (Input.GetKeyDown(KeyCode.Escape))
        QuitGame();
}

public void Play()
{
    score = 0;
    scoreText.text = score.ToString();

    opponentScore = 0;
    if (opponentScoreText != null)
        opponentScoreText.text = opponentScore.ToString();

    pipesSpawnedThisRound = 0;
    jumpsThisRound = 0;
    roundElapsed = 0f;
    roundStartUtc = DateTime.UtcNow;


    playButton.SetActive(false);
    gameOver.SetActive(false);
    readyButton.SetActive(false);
    if (difficultyImage != null) difficultyImage.gameObject.SetActive(false);

    Time.timeScale = 1f;
    player.enabled = true;

    // Clean up all spawned obstacles and collectibles
    foreach (var p in FindObjectsOfType<Pipes>())
        Destroy(p.gameObject);
    foreach (var t in FindObjectsOfType<Tornado>())
        Destroy(t.gameObject);
    foreach (var b in FindObjectsOfType<Balloon>())
        Destroy(b.gameObject);
    foreach (var s in FindObjectsOfType<Silo>())
        Destroy(s.gameObject);
    foreach (var t in FindObjectsOfType<Turbine>())
        Destroy(t.gameObject);
    foreach (var c in FindObjectsOfType<CycloneBird>())
        Destroy(c.gameObject);
    foreach (var ck in FindObjectsOfType<CornKernel>())
        Destroy(ck.gameObject);
    foreach (var h in FindObjectsOfType<Helmet>())
        Destroy(h.gameObject);
    foreach (var f in FindObjectsOfType<Football>())
        Destroy(f.gameObject);
    foreach (var gp in FindObjectsOfType<GoalPost>())
        Destroy(gp.gameObject);
    
    // Reset spawner to spawn parent tornado if needed
    var spawner = FindObjectOfType<Spawner>();
    if (spawner != null)
        spawner.ResetSpawner();
    
    // Apply appropriate settings based on game mode
    if (currentGameMode == GameMode.GameDay)
    {
        ApplyGameDaySettings();
    }
    else
    {
        ApplyDifficulty();
    }
    
    // Initialize Game Day Mode if active
    var gameDayMgr = FindObjectOfType<GameDayManager>();
    if (gameDayMgr != null)
        gameDayMgr.ResetGameDayRound();
}

public void GameOver()
{
    gameOver.SetActive(true);
    playButton.SetActive(true);
    readyButton.SetActive(false);
    if (difficultyImage != null) difficultyImage.gameObject.SetActive(true);
    if (quitButton != null) quitButton.SetActive(true);
    if (dropdownMenu != null) dropdownMenu.SetActive(true);
    Pause();

    // Reset Game Day mode to Offense if active
    var gameDayMgr = FindObjectOfType<GameDayManager>();
    if (gameDayMgr != null)
    {
        gameDayMgr.ResetGameDayOnGameOver();
    }

    // Clean up all spawned obstacles and collectibles
    foreach (var p in FindObjectsOfType<Pipes>())
        Destroy(p.gameObject);
    foreach (var t in FindObjectsOfType<Tornado>())
        Destroy(t.gameObject);
    foreach (var b in FindObjectsOfType<Balloon>())
        Destroy(b.gameObject);
    foreach (var s in FindObjectsOfType<Silo>())
        Destroy(s.gameObject);
    foreach (var t in FindObjectsOfType<Turbine>())
        Destroy(t.gameObject);
    foreach (var c in FindObjectsOfType<CycloneBird>())
        Destroy(c.gameObject);
    foreach (var ck in FindObjectsOfType<CornKernel>())
        Destroy(ck.gameObject);
    foreach (var h in FindObjectsOfType<Helmet>())
        Destroy(h.gameObject);
    foreach (var f in FindObjectsOfType<Football>())
        Destroy(f.gameObject);
    foreach (var gp in FindObjectsOfType<GoalPost>())
        Destroy(gp.gameObject);

    RunDataLogger.AppendRun(
            playerId: RunDataLogger.PlayerId,
            difficulty: currentDifficulty,
            score: score,
            roundSeconds: roundElapsed,
            startUtc: roundStartUtc,
            pipesSpawned: pipesSpawnedThisRound,
            jumps: jumpsThisRound
        );
}

    public void Pause()
    {
        Time.timeScale = 0f;
        player.enabled = false;
    }


    public void ChangeDifficulty()
    {
        currentDifficulty = (Difficulty)(((int)currentDifficulty + 1) % 3);
        Debug.Log($"[GameManager] Difficulty changed to: {currentDifficulty}");
        ApplyDifficulty();
    }

    public void SetDifficulty(int difficultyIndex)
    {
        if (difficultyIndex >= 0 && difficultyIndex < 3)
        {
            currentDifficulty = (Difficulty)difficultyIndex;
            ApplyDifficulty();
        }
    }

    public float CurrentSpawnRate { get; private set; }

    private void ApplyDifficulty()
{
    float spawnRate;
    Sprite currentSprite;
    switch (currentDifficulty)
    {
        case Difficulty.Normal:
            spawnRate = normalSpawnRate;
            currentSprite = normalSprite;
            break;
        case Difficulty.Hard:
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

    foreach (Pipes p in FindObjectsOfType<Pipes>())
        p.pipeSpeed = pipeSpeed;
    OnPipeSpeedChanged?.Invoke(pipeSpeed);

    // set and broadcast spawn rate
    CurrentSpawnRate = spawnRate;             // <-- remember it
    OnSpawnRateChanged?.Invoke(spawnRate);    // <-- broadcast

    // Broadcast difficulty change for turbines and other obstacles
    OnDifficultyChanged?.Invoke(currentDifficulty);

    if (difficultyImage != null)
        difficultyImage.sprite = currentSprite;
}

private void ApplyGameDaySettings()
{
    CurrentPipeSpeed = pipeSpeed;
    player.gravity = -9.8f;

    foreach (Pipes p in FindObjectsOfType<Pipes>())
        p.pipeSpeed = pipeSpeed;
    OnPipeSpeedChanged?.Invoke(pipeSpeed);

    CurrentSpawnRate = gameDaySpawnRate;
    OnSpawnRateChanged?.Invoke(gameDaySpawnRate);

    if (difficultyImage != null)
        difficultyImage.gameObject.SetActive(false);
}

    public void QuitGame()
{
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
}

public void RegisterJump()
{
    jumpsThisRound++;
}

public void RegisterPipe()
{
    pipesSpawnedThisRound++;
}

public void OnPlayerDamaged(int remainingHealth)
{
    // Called when player takes damage
    // remainingHealth is the health after damage
    // You can add UI updates here (e.g., health bar, damage flash effect)
}

public void OnPlayerHealed(int newHealth)
{
    // Called when player gains health
    // You can add UI updates here (e.g., health bar, heal effect)
}

public void SetGameMode(GameMode mode)
{
    currentGameMode = mode;
}

public void SetGameDayDifficulty(GameDayDifficulty difficulty)
{
    gameDayDifficulty = difficulty;
}

public int GetOpponentScore()
{
    return opponentScore;
}

}
