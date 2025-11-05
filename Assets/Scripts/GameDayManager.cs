using UnityEngine;
using TMPro;
using System;
using System.Collections;

public class GameDayManager : MonoBehaviour
{
    public static GameDayManager Instance { get; private set; }

    // Bridge events so other scripts can still react to speed/spawn changes
    public static event Action<float> OnPipeSpeedChanged;
    public static event Action<float> OnSpawnRateChanged;

    public float CurrentPipeSpeed { get; private set; } = 5f;
    public float CurrentSpawnRate { get; private set; } = 1.2f;
    public GameManager.GameDayDifficulty CurrentGameDayDifficulty { get; private set; } = GameManager.GameDayDifficulty.College;

    [Header("UI")]
    public TextMeshProUGUI modeText;

    [Header("Settings")]
    public float goalPostSpawnX = 12f;
    public float defenseRoundDuration = 10f;

    private bool inDefenseRound = false;
    private bool isSpawningPaused = false;
    private bool ballCarrierSpawning = false;
    public bool InDefenseRound => inDefenseRound;

    private Spawner spawner;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // load saved difficulty if available
        if (PlayerPrefs.HasKey("GameDayDifficulty"))
            CurrentGameDayDifficulty = (GameManager.GameDayDifficulty)PlayerPrefs.GetInt("GameDayDifficulty");
    }

    private void OnEnable()
    {
        spawner = FindObjectOfType<Spawner>();
        UpdateModeDisplay();
    }

    private void Update()
    {
        UpdateModeDisplay();
    }

    private void UpdateModeDisplay()
    {
        if (modeText != null)
        {
            modeText.text = inDefenseRound ? "DEFENSE" : "OFFENSE";
            modeText.gameObject.SetActive(true);
        }
    }

    // ------------------- GAME FLOW -------------------

    public bool IsInDefenseRound() => inDefenseRound;
    public bool IsBallCarrierSpawningThisFrame() => ballCarrierSpawning;
    public bool IsSpawningPaused() => isSpawningPaused;

    public void StartDefenseRound()
    {
        if (inDefenseRound) return;
        Debug.Log("[GameDayManager] Starting Defense Round...");
        inDefenseRound = true;
        isSpawningPaused = false;
        StartCoroutine(DefenseRoundTimer());
    }

    public void EndDefenseRound(bool playerWon)
    {
        Debug.Log($"[GameDayManager] Defense round ended. Player won? {playerWon}");
        inDefenseRound = false;
        ballCarrierSpawning = false;

        if (!playerWon)
        {
            IncreaseOpponentScore(1);
        }

        // Reset for next offense round
        if (spawner != null)
            spawner.ResetGameDayBall();
    }

    private IEnumerator DefenseRoundTimer()
    {
        yield return new WaitForSeconds(defenseRoundDuration);
        if (inDefenseRound)
        {
            Debug.Log("[GameDayManager] Defense round timed out!");
            EndDefenseRound(false);
        }
    }

    public void OnBallCarrierSpawned()
    {
        Debug.Log("[GameDayManager] Ball carrier spawned!");
        ballCarrierSpawning = true;
        isSpawningPaused = true;
    }

    public void OnBallCarrierDespawned()
    {
        Debug.Log("[GameDayManager] Ball carrier despawned — defense failed.");
        ballCarrierSpawning = false;
        inDefenseRound = false;
        IncreaseOpponentScore(1);
    }

    public void OnWaveCompleted()
    {
        Debug.Log("[GameDayManager] Wave completed!");
    }

    public void IncreaseOpponentScore(int amount = 1)
    {
        Debug.Log($"[GameDayManager] Opponent scored {amount} point(s).");
    }

    public void SetGameDayDifficulty(GameManager.GameDayDifficulty diff)
    {
        CurrentGameDayDifficulty = diff;
        PlayerPrefs.SetInt("GameDayDifficulty", (int)diff);
        PlayerPrefs.Save();
    }

    // just to match references in Spawner/Football
    public void IncreaseScore(int amount = 1)
    {
        Debug.Log($"[GameDayManager] Player scored {amount} point(s).");
    }
}
