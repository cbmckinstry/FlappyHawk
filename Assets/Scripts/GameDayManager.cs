using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class GameDayManager : MonoBehaviour
{
    public static GameDayManager Instance { get; private set; }

    [Header("Scene References")]
    public Player player;
    public GameObject playButton;
    public GameObject gameOver;
    public GameObject readyButton;
    public GameObject menuButton;
    public Image difficultyImage;
    public Sprite collegeSprite;
    public Sprite proSprite;

    // UI (internal labels)
    [SerializeField] private TextMeshProUGUI modeText;
    [SerializeField] private TextMeshProUGUI playerScoreText;
    [SerializeField] private TextMeshProUGUI enemyScoreText;

    [Header("Tuning")]
    [SerializeField] private float scrollSpeed = 5f;
    [SerializeField] private float collegeSpawnRate = 1.10f;
    [SerializeField] private float proSpawnRate = 0.90f;

    [Header("GameDay Settings")]
    public float goalPostSpawnX = 12f;
    public float defenseRoundDuration = 10f;

    // Difficulty + events
    public GameManager.GameDayDifficulty CurrentGameDayDifficulty { get; private set; } =
        GameManager.GameDayDifficulty.College;

    public static event Action<float> OnScrollSpeedChanged;
    public static event Action<float> OnSpawnRateChanged;

    public float CurrentScrollSpeed { get; private set; } = 5f;
    public float CurrentSpawnRate { get; private set; } = 1.2f;

    // Round/score state
    private bool inDefenseRound = false;
    private bool isSpawningPaused = false;
    private bool ballCarrierSpawning = false;
    public bool InDefenseRound => inDefenseRound;

    private Spawner spawner;
    private int playerScore = 0;
    private int enemyScore = 0;

    private DateTime roundStartUtc;
    private float roundElapsed;
    private int obstaclesSpawned;
    private int jumps;


    // -------------------- Unity Lifecycle --------------------
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Load saved difficulty if available
        if (PlayerPrefs.HasKey("GameDayDifficulty"))
            CurrentGameDayDifficulty = (GameManager.GameDayDifficulty)PlayerPrefs.GetInt("GameDayDifficulty");

        gameOver?.SetActive(false);
        Pause();
    }

    private void OnEnable()
    {
        spawner = FindObjectOfType<Spawner>();
        UpdateModeDisplay();

        // <<< Force a true reset on load >>>
        ResetScores();                  // sets both scores = 0 and updates UI
    }

    private void Start()
    {
        SelectPlayButton();
    }

    private void Update()
    {
        UpdateModeDisplay();
    }

    // -------------------- UI Helpers --------------------
    private void UpdateModeDisplay()
    {
        if (modeText != null)
        {
            modeText.text = inDefenseRound ? "DEFENSE" : "OFFENSE";
            if (!modeText.gameObject.activeSelf) modeText.gameObject.SetActive(true);
        }
    }

    private void SetPlayerScoreUI(int value)
    {
        if (playerScoreText != null)
        {
            playerScoreText.text = value.ToString();
            if (!playerScoreText.gameObject.activeSelf) playerScoreText.gameObject.SetActive(true);
        }
    }

    private void SetEnemyScoreUI(int value)
    {
        if (enemyScoreText == null)
            enemyScoreText = FindObjectOfType<TextMeshProUGUI>(true); // or Find by name/tag you use

        if (enemyScoreText != null)
        {
            enemyScoreText.text = value.ToString();
            if (!enemyScoreText.gameObject.activeSelf) enemyScoreText.gameObject.SetActive(true);
        }
    }


    // -------------------- Queries (used by Spawner/etc.) --------------------
    public bool IsInDefenseRound() => inDefenseRound;
    public bool IsBallCarrierSpawningThisFrame() => ballCarrierSpawning;
    public bool IsSpawningPaused() => isSpawningPaused;

    // -------------------- Flow Control --------------------
    public void StartDefenseRound()
    {
        if (inDefenseRound) return;

        Debug.Log("[GameDayManager] Starting Defense Round...");
        inDefenseRound = true;
        isSpawningPaused = false;   // allow spawner to run prep logic if needed
        StartCoroutine(DefenseRoundTimer());
    }

    public void EndDefenseRound(bool playerWon)
    {
        Debug.Log($"[GameDayManager] Defense round ended. Player won? {playerWon}");

        // Clear defense flags
        inDefenseRound = false;
        ballCarrierSpawning = false;
        isSpawningPaused = false;

        // Centralize enemy scoring for defense failure HERE
        if (!playerWon)
        {
            int pointsScored = Random.value < 0.7f ? 3 : 7;
            enemyScore += pointsScored;
            SetEnemyScoreUI(enemyScore);
            Debug.Log($"[GameDayManager] Opponent scored {pointsScored}. Opponent total: {enemyScore}");
        }

        // Reset offense drive state for next wave/ball
        if (spawner != null)
            spawner.ResetGameDayBall();
    }

    private IEnumerator DefenseRoundTimer()
    {
        yield return new WaitForSeconds(defenseRoundDuration);

        if (inDefenseRound)
        {
            Debug.Log("[GameDayManager] Defense round timed out!");
            EndDefenseRound(false); // counts as enemy point
        }
    }

    public void OnBallCarrierSpawned()
    {
        Debug.Log("[GameDayManager] Ball carrier spawned!");
        ballCarrierSpawning = true;
        isSpawningPaused = true; // pause regular spawns while the carrier is active
    }

    public void OnBallCarrierDespawned()
    {
        // Do NOT score here to avoid double-count. Funnel to EndDefenseRound(false).
        Debug.Log("[GameDayManager] Ball carrier despawned — defense failed.");
        EndDefenseRound(false);
    }

    public void OnWaveCompleted()
    {
        Debug.Log("[GameDayManager] Wave completed!");
    }

    // -------------------- Scoring API --------------------
    /// <summary>
    /// Enemy scoring (turnovers, defense failures, etc.)
    /// </summary>
    public void IncreaseOpponentScore(int amount = 1)
    {
        if (amount <= 0) return;
        enemyScore += amount;
        SetEnemyScoreUI(enemyScore);
        Debug.Log($"[GameDayManager] Opponent scored {amount}. Enemy total: {enemyScore}");
    }

    /// <summary>
    /// Player scoring. Kept name to match existing calls in Spawner/Football.
    /// </summary>
    public void IncreaseScore(int amount = 1)
    {
        if (amount <= 0) return;
        playerScore += amount;
        SetPlayerScoreUI(playerScore);
        Debug.Log($"[GameDayManager] Player scored {amount}. Player total: {playerScore}");

        // Play different sounds depending on score type
        if (amount == 7)
            AudioManager.Instance?.PlayTouchdown();
        else if (amount == 3)
            AudioManager.Instance?.PlayFieldGoal();

    }

    /// <summary>
    /// Reset only the numeric scores and update UI (does not touch round flags).
    /// </summary>
    public void ResetScores()
    {
        playerScore = 0;
        enemyScore = 0;
        SetPlayerScoreUI(playerScore);
        SetEnemyScoreUI(enemyScore);
    }

    /// <summary>
    /// Call this when the player dies (from GameManager or Player).
    /// Fully resets scores and spawner/round flags so the next life starts clean.
    /// </summary>
    public void OnPlayerDeathReset()
    {
        // Reset scores
        ResetScores();

        // Clear round/spawn flags
        inDefenseRound = false;
        ballCarrierSpawning = false;
        isSpawningPaused = false;

        // Reset spawner fully so offense can start again
        if (spawner != null)
            spawner.ResetSpawner();
    }

    // -------------------- Difficulty --------------------
    public void SetGameDayDifficulty(GameManager.GameDayDifficulty diff)
    {
        CurrentGameDayDifficulty = diff;
        PlayerPrefs.SetInt("GameDayDifficulty", (int)diff);
        PlayerPrefs.Save();
    }

    // -------------------- Optional broadcasters (if you ever change speeds) --------------------
    public void SetScrollSpeed(float speed)
    {
        CurrentScrollSpeed = Mathf.Max(0f, speed);
        OnScrollSpeedChanged?.Invoke(CurrentScrollSpeed);
    }

    public void SetSpawnRate(float rate)
    {
        CurrentSpawnRate = Mathf.Max(0.05f, rate);
        OnSpawnRateChanged?.Invoke(CurrentSpawnRate);
    }

    public bool IsGameActive()
    {
        // If you ever add a pause menu, this will correctly detect it.
        return !isSpawningPaused || inDefenseRound;
    }


    // =========================================================
    // ==========  NEW METHODS FOR STANDALONE GAMEDAY ==========
    // =========================================================

    private void ApplyDifficulty()
    {
        float spawnRate;
        Sprite spriteToUse;

        switch (CurrentGameDayDifficulty)
        {
            case GameManager.GameDayDifficulty.Pro:
                spawnRate = proSpawnRate;
                spriteToUse = proSprite;
                break;
            default:
                spawnRate = collegeSpawnRate;
                spriteToUse = collegeSprite;
                break;
        }

        CurrentScrollSpeed = scrollSpeed;
        CurrentSpawnRate = spawnRate;
        OnScrollSpeedChanged?.Invoke(scrollSpeed);
        OnSpawnRateChanged?.Invoke(spawnRate);

        if (difficultyImage != null)
            difficultyImage.sprite = spriteToUse;
    }

    public void Play()
    {
        player.enabled = true;
        playButton?.SetActive(false);
        gameOver?.SetActive(false);
        readyButton?.SetActive(false);
        menuButton?.SetActive(false);
        difficultyImage?.gameObject.SetActive(false);

        Time.timeScale = 1f;

        ResetScores();
        OnPlayerDeathReset();

        ApplyDifficulty();
    }

    public void GameOver()
    {
        gameOver?.SetActive(true);
        playButton?.SetActive(true);
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

    public void Pause()
    {
        Time.timeScale = 0f;
        player.enabled = false;
    }

    public void ReturnToMainMenu()
    {
        AudioManager.Instance?.PlayClickSound();
        Time.timeScale = 1f;
        SceneManager.LoadScene("MenuScreen");
    }
}
