using System;using System;
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
    public TMP_InputField playerNameInput;

    // UI (internal labels)
    [SerializeField] private TextMeshProUGUI modeText;
    private TextMeshProUGUI playerScoreText;
    private TextMeshProUGUI opponentScoreText;
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

    private Coroutine modePopupRoutine;

    // Round/score state
    private bool inDefenseRound = false;
    private bool isSpawningPaused = false;
    private bool ballCarrierSpawning = false;
    public bool InDefenseRound => inDefenseRound;

    private Spawner spawner;
    private int playerScore = 0;
    private int enemyScore = 0;

    // TIMERS / COUNTERS
    private float roundElapsed = 0f;
    private int obstaclesSpawned = 0;
    private int jumps = 0;

    // Logging counters 
    private int offenseDrives = 0;
    private int defenseRoundsWon = 0;
    private int defenseRoundsFailed = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (PlayerPrefs.HasKey("GameDayDifficulty"))
            CurrentGameDayDifficulty = (GameManager.GameDayDifficulty)PlayerPrefs.GetInt("GameDayDifficulty");

        gameOver?.SetActive(false);

        FindAndCacheScoreTextReferences();

        if (player != null)
            player.gameObject.SetActive(false);

        Pause();
    }

    private void OnEnable()
    {
        spawner = FindObjectOfType<Spawner>();

        UpdateModeDisplay(false);

        ResetScores();

        roundElapsed = 0f;
        jumps = 0;
        obstaclesSpawned = 0;
    }

    private void Start()
    {
        SelectPlayButton();
    }

    private void Update()
    {
        if (IsGameActive())
        {
            roundElapsed += Time.unscaledDeltaTime;

            bool jumpPressed =
                (Keyboard.current?.spaceKey.wasPressedThisFrame ?? false) ||
                (Mouse.current?.leftButton.wasPressedThisFrame ?? false) ||
                (Gamepad.current?.buttonSouth.wasPressedThisFrame ?? false);

            if (jumpPressed) jumps++;
        }
    }

    private void FindAndCacheScoreTextReferences()
    {
        GameObject playerScoreObj = GameObject.Find("PlayerScore");
        if (playerScoreObj != null)
            playerScoreText = playerScoreObj.GetComponent<TextMeshProUGUI>();

        GameObject opponentScoreObj = GameObject.Find("OpponentScore");
        if (opponentScoreObj != null)
            opponentScoreText = opponentScoreObj.GetComponent<TextMeshProUGUI>();
    }

    private void UpdateModeDisplay(bool playPopup = true)
    {
        if (modeText == null) return;

        modeText.text = inDefenseRound ? "DEFENSE" : "OFFENSE";

        if (playPopup)
            ShowModePopup();
    }

    private void UpdatePlayerScoreUI(int value)
    {
        if (playerScoreText != null)
            playerScoreText.text = value.ToString();
    }

    private void UpdateOpponentScoreUI(int value)
    {
        if (opponentScoreText != null)
            opponentScoreText.text = value.ToString();
    }

    // -------------------- RESTORED PUBLIC API --------------------
    public bool IsInDefenseRound()
    {
        return inDefenseRound;
    }

    public bool IsSpawningPaused()
    {
        return isSpawningPaused;
    }

    public bool IsBallCarrierSpawningThisFrame()
    {
        return ballCarrierSpawning;
    }

    public void OnWaveCompleted()
    {
        Debug.Log("[GameDay] Wave completed");
    }
    // ------------------------------------------------------------

    public bool IsGameActive()
    {
        return Time.timeScale > 0f && player != null && player.enabled;
    }

    public void StartDefenseRound()
    {
        AudioManager.Instance?.PlayWhistle();

        if (inDefenseRound) return;

        inDefenseRound = true;
        isSpawningPaused = false;

        StartCoroutine(DefenseRoundTimer());

        UpdateModeDisplay(true);
    }

    public void EndDefenseRound(bool playerWon)
    {
        inDefenseRound = false;
        ballCarrierSpawning = false;
        isSpawningPaused = false;

        UpdateModeDisplay(true);

        if (playerWon)
            defenseRoundsWon++;

        else
        {
            AudioManager.Instance?.PlayEnemyScore();

            defenseRoundsFailed++;

            int pointsScored = UnityEngine.Random.value < 0.7f ? 3 : 7;
            enemyScore += pointsScored;
            UpdateOpponentScoreUI(enemyScore);
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

    public void IncreaseOpponentScore(int amount = 1)
    {
        if (amount <= 0) return;
        enemyScore += amount;
        UpdateOpponentScoreUI(enemyScore);
    }

    public void IncreaseScore(int amount = 1)
    {
        if (amount <= 0) return;
        playerScore += amount;
        UpdatePlayerScoreUI(playerScore);
    }

    public void ResetScores()
    {
        playerScore = 0;
        enemyScore = 0;
        UpdatePlayerScoreUI(0);
        UpdateOpponentScoreUI(0);
    }

    public void SetGameDayDifficulty(GameManager.GameDayDifficulty diff)
    {
        CurrentGameDayDifficulty = diff;
        PlayerPrefs.SetInt("GameDayDifficulty", (int)diff);
        PlayerPrefs.Save();
    }

    private void ApplyDifficulty()
    {
        float spawnRate;

        switch (CurrentGameDayDifficulty)
        {
            case GameManager.GameDayDifficulty.Pro:
                spawnRate = proSpawnRate;
                break;

            default:
                spawnRate = collegeSpawnRate;
                break;
        }

        CurrentScrollSpeed = scrollSpeed;
        CurrentSpawnRate = spawnRate;

        OnScrollSpeedChanged?.Invoke(scrollSpeed);
        OnSpawnRateChanged?.Invoke(spawnRate);
    }

    public void Play()
    {
        if (player != null)
            player.gameObject.SetActive(true);

        player.enabled = true;

        playButton?.SetActive(false);
        gameOver?.SetActive(false);
        readyButton?.SetActive(false);
        menuButton?.SetActive(false);
        playerNameInput?.gameObject.SetActive(false);

        Time.timeScale = 1f;

        ResetScores();

        roundElapsed = 0f;
        jumps = 0;
        obstaclesSpawned = 0;

        OnPlayerDeathReset();
        UpdateAllDisplays();
        ApplyDifficulty();

        UpdateModeDisplay(true);
    }

    public void GameOver()
    {
        LogGameDayRun();


        if (player != null)
            player.gameObject.SetActive(false);

        gameOver?.SetActive(true);
        playButton.SetActive(true);
        readyButton?.SetActive(false);
        menuButton.SetActive(true);

        spawner?.ClearAllGameDayActors();

        Pause();
        SelectPlayButton();
    }

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

            score = 0, // Gameday does NOT use Iowa score
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

    private void ShowModePopup()
    {
        if (modePopupRoutine != null)
            StopCoroutine(modePopupRoutine);

        modePopupRoutine = StartCoroutine(ModePopupRoutine());
    }

    private IEnumerator ModePopupRoutine()
    {
        modeText.gameObject.SetActive(true);

        yield return new WaitForSeconds(2f);

        modeText.gameObject.SetActive(false);
    }
}
