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
    public TMP_InputField playerNameInput;

    // UI (internal labels)
    [SerializeField] private TextMeshProUGUI modeText;
    [SerializeField] private TextMeshProUGUI playerScoreText;
    [SerializeField] private TextMeshProUGUI enemyScoreText;
    [SerializeField] private TextMeshProUGUI helmetDurabilityText;
    [SerializeField] private TextMeshProUGUI playerHealthText;

    [Header("Tuning")]
    [SerializeField] private float scrollSpeed = 5f;
    [SerializeField] private float collegeSpawnRate = 1.10f;
    [SerializeField] private float proSpawnRate = 0.90f;

    [Header("GameDay Settings")]
    public float goalPostSpawnX = 12f;
    public float defenseRoundDuration = 10f;

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

    // Logging counters 
    private int offenseDrives = 0;
    private int defenseRoundsWon = 0;
    private int defenseRoundsFailed = 0;



    // -------------------- Unity Lifecycle --------------------
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (PlayerPrefs.HasKey("GameDayDifficulty"))
            CurrentGameDayDifficulty = (GameManager.GameDayDifficulty)PlayerPrefs.GetInt("GameDayDifficulty");

        gameOver?.SetActive(false);
        Pause();
    }

    private void OnEnable()
    {
        spawner = FindObjectOfType<Spawner>();
        UpdateModeDisplay();
        ResetScores();
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
            enemyScoreText = FindObjectOfType<TextMeshProUGUI>(true);

        if (enemyScoreText != null)
        {
            enemyScoreText.text = value.ToString();
            if (!enemyScoreText.gameObject.activeSelf) enemyScoreText.gameObject.SetActive(true);
        }
    }

    // -------------------- Queries --------------------
    public bool IsInDefenseRound() => inDefenseRound;
    public bool IsBallCarrierSpawningThisFrame() => ballCarrierSpawning;
    public bool IsSpawningPaused() => isSpawningPaused;

    // *** ADDED BACK — required by GameManager ***
    public bool IsGameActive()
    {
        return Time.timeScale > 0f && player != null && player.enabled;
    }


    // -------------------- Flow Control --------------------
    public void StartDefenseRound()
    {
        if (inDefenseRound) return;

        inDefenseRound = true;
        isSpawningPaused = false;
        StartCoroutine(DefenseRoundTimer());
    }

    public void EndDefenseRound(bool playerWon)
    {
        inDefenseRound = false;
        ballCarrierSpawning = false;
        isSpawningPaused = false;

        if (playerWon)
            defenseRoundsWon++;
        else
        {
            defenseRoundsFailed++;

            int pointsScored = UnityEngine.Random.value < 0.7f ? 3 : 7;
            enemyScore += pointsScored;
            SetEnemyScoreUI(enemyScore);
        }

        spawner?.ResetGameDayBall();
    }

    private IEnumerator DefenseRoundTimer()
    {
        yield return new WaitForSeconds(defenseRoundDuration);

        if (inDefenseRound)
            EndDefenseRound(false);
    }

    public void OnBallCarrierSpawned()
    {
        ballCarrierSpawning = true;
        isSpawningPaused = true;
        offenseDrives++;
    }

    public void OnBallCarrierDespawned()
    {
        EndDefenseRound(false);
    }

    public void OnWaveCompleted()
    {
        Debug.Log("[GameDay] Wave completed");
    }

    // -------------------- Scoring API --------------------
    public void IncreaseOpponentScore(int amount = 1)
    {
        if (amount <= 0) return;
        enemyScore += amount;
        SetEnemyScoreUI(enemyScore);
    }

    public void IncreaseScore(int amount = 1)
    {
        if (amount <= 0) return;
        playerScore += amount;
        SetPlayerScoreUI(playerScore);
    }

    public void ResetScores()
    {
        playerScore = 0;
        enemyScore = 0;
        SetPlayerScoreUI(0);
        SetEnemyScoreUI(0);
    }

    // -------------------- Difficulty --------------------
    public void SetGameDayDifficulty(GameManager.GameDayDifficulty diff)
    {
        CurrentGameDayDifficulty = diff;
        PlayerPrefs.SetInt("GameDayDifficulty", (int)diff);
        PlayerPrefs.Save();
    }

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

        difficultyImage.sprite = spriteToUse;
    }

    // -------------------- Play / GameOver --------------------
    public void Play()
    {
        player.enabled = true;

        playButton?.SetActive(false);
        gameOver?.SetActive(false);
        readyButton?.SetActive(false);
        menuButton?.SetActive(false);
        difficultyImage?.gameObject.SetActive(false);
        playerNameInput?.gameObject.SetActive(false);

        Time.timeScale = 1f;

        ResetScores();
        OnPlayerDeathReset();
        UpdateAllDisplays();

        ApplyDifficulty();
    }

    public void GameOver()
    {
        LogGameDayRun();

        gameOver?.SetActive(true);
        playButton?.SetActive(true);
        readyButton?.SetActive(false);
        difficultyImage?.gameObject.SetActive(true);
        menuButton?.SetActive(true);

        spawner?.ClearAllGameDayActors();

        Pause();
        SelectPlayButton();
    }

    // *** ADDED BACK — required because Play(), GameOver() call it ***
    private void SelectPlayButton()
    {
        Button button = playButton?.GetComponent<Button>();
        if (button != null)
            EventSystem.current?.SetSelectedGameObject(button.gameObject);
    }


    public void Pause()
    {
        Time.timeScale = 0f;
        player.enabled = false;
        isSpawningPaused = true;
    }

    public void OnPlayerDeathReset()
    {
        ResetScores();

        inDefenseRound = false;
        ballCarrierSpawning = false;
        isSpawningPaused = false;

        spawner?.ClearAllGameDayActors();
        spawner?.ResetSpawner();
    }

    // -------------------- UI Updates --------------------
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

    // -------------------- Logging --------------------
    private string GetFinalizedPlayerName()
    {
        if (playerNameInput == null) return "Unknown";

        playerNameInput.DeactivateInputField();
        playerNameInput.ForceLabelUpdate();

        string name = playerNameInput.text.Trim();
        return string.IsNullOrEmpty(name) ? "Unknown" : name;
    }

    private void LogGameDayRun()
    {
        RunLogData data = new RunLogData
        {
            playerName = GetFinalizedPlayerName(),

            gameMode = "GameDay",
            difficulty = CurrentGameDayDifficulty.ToString(),

            score = playerScore,
            playerScore = playerScore,
            enemyScore = enemyScore,

            roundSeconds = roundElapsed,

            obstaclesSpawned = obstaclesSpawned,
            jumps = jumps,
            helmetsCollected = 0,

            offenseDrives = offenseDrives,
            defenseRoundsWon = defenseRoundsWon,
            defenseRoundsFailed = defenseRoundsFailed
        };

        RunDataLogger.AppendRun(data);
    }
}
