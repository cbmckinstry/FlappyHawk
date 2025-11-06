using UnityEngine;
using TMPro;
using System;
using System.Collections;

public class GameDayManager : MonoBehaviour
{
    public static GameDayManager Instance { get; private set; }

    [Header("Spawn Settings")]
    public float goalPostSpawnX = 12f;

    [Header("UI")]
    public TextMeshProUGUI modeText; // Text to display Offense/Defense mode

    private bool inDefenseRound = false;
    private bool playerHasFootball = false;
    private int wavesCompletedForCurrentGoal = 0;
    private int wavesBetweenGoals = 10; // Always 10 waves
    private Spawner spawner;
    private float defenseRoundTimeout = 10f;
    private float defenseRoundTimer = 0f;
    private bool ballCarrierActive = false;
    private bool isSpawningPaused = false;
    private bool ballCarrierSpawnedThisDefense = false;
    private float ballCarrierSpawnDelay = 10f; // Spawn ball carrier after 10 seconds
    private float defenseStartTime = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        spawner = FindObjectOfType<Spawner>();
        UpdateModeDisplay();
    }

    private void Start()
    {
        // Waves are always 10 for both offense and defense
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

    private void Update()
    {
        if (inDefenseRound)
        {
            defenseRoundTimer -= Time.deltaTime;
            
            // Spawn ball carrier after 10 seconds of defense round
            float elapsedTime = defenseRoundTimeout - defenseRoundTimer;
            if (!ballCarrierSpawnedThisDefense && elapsedTime >= ballCarrierSpawnDelay)
            {
                SpawnBallCarrierAfterDelay();
                ballCarrierSpawnedThisDefense = true;
            }
            
            // Defense round continues until ball carrier is either hit or despawns
            // No automatic time limit
        }

        // Check if player is carrying football
        Football football = FindObjectOfType<Football>();
        bool wasCarrying = playerHasFootball;
        playerHasFootball = (football != null && football.IsCarried());
        
        if (football != null && playerHasFootball != wasCarrying)
        {
            Debug.Log($"[GameDayManager] Football carry state changed to: {playerHasFootball}");
        }

        // Check if we should spawn a goal post
        CheckAndSpawnGoalPost();
    }

    private void CheckAndSpawnGoalPost()
    {
        if (inDefenseRound)
            return;

        // Log goal post spawn attempt info
        if (playerHasFootball && wavesCompletedForCurrentGoal > 0)
        {
            Debug.Log($"[GameDayManager] Waves completed: {wavesCompletedForCurrentGoal}/{wavesBetweenGoals}, PlayerHasFootball: {playerHasFootball}");
        }
    }

    public void ResetGameDayRound()
    {
        inDefenseRound = false;
        playerHasFootball = false;
        wavesCompletedForCurrentGoal = 0;
        wavesBetweenGoals = 10; // Always 10 waves
        defenseRoundTimer = 0f;
        ballCarrierActive = false;
        ballCarrierSpawnedThisDefense = false;
        
        // Reset ball spawning for next offense round
        if (spawner != null)
        {
            spawner.ResetGameDayBall();
        }
    }

    public void StartDefenseRound()
    {
        inDefenseRound = true;
        defenseRoundTimer = defenseRoundTimeout;
        ballCarrierActive = false;
        ballCarrierSpawnedThisDefense = false;
        UpdateModeDisplay();

        Debug.Log("Defense round started!");
    }

    public void EndDefenseRound(bool playerWon)
    {
        inDefenseRound = false;
        defenseRoundTimer = 2f;

        if (playerWon)
        {
            Debug.Log("Player won defense round! Offense continues.");
            // Reset wave counter for next goal
            wavesCompletedForCurrentGoal = 0;
            
            // Use coroutine for smooth transition
            StartCoroutine(TransitionToOffenseMode());
        }
        else
        {
            // Pause spawning during transition
            isSpawningPaused = true;
            
            // Opponent scores
            bool isHighScore = UnityEngine.Random.value < 0.3f; // 30% chance for 7 points
            int opponentPoints = isHighScore ? 7 : 3;
            
            FindObjectOfType<GameManager>().IncreaseOpponentScore(opponentPoints);
            Debug.Log("Opponent scored " + opponentPoints + " points!");

            // Reset wave counter for next goal
            wavesCompletedForCurrentGoal = 0;
            
            UpdateModeDisplay();
            
            // Resume spawning and swap back to normal round (offense)
            Debug.Log("Returning to normal round...");
            isSpawningPaused = false;
            ResetGameDayRound();
        }
    }

    private IEnumerator TransitionToOffenseMode()
    {
        // Pause spawning during transition
        isSpawningPaused = true;
        
        // Pause for a moment
        yield return new WaitForSeconds(2f);
        
        // Update mode text to show OFFENSE
        UpdateModeDisplay();
        
        // Pause again before resuming spawning
        yield return new WaitForSeconds(2f);
        
        // Resume spawning BEFORE resetting so ball can spawn
        isSpawningPaused = false;
        
        // Reset to offense mode - ball will spawn in next frame
        ResetGameDayRound();
    }

    public void OnBallCarrierSpawned()
    {
        ballCarrierActive = true;
    }

    public void OnBallCarrierDespawned()
    {
        // Ball carrier despawned without being hit = opponent scores
        if (ballCarrierActive && inDefenseRound)
        {
            Debug.Log("[GameDayManager] Ball carrier despawned! Opponent scores!");
            EndDefenseRound(false);
        }
        ballCarrierActive = false;
    }

    public void OnWaveCompleted()
    {
        if (inDefenseRound)
            return;

        wavesCompletedForCurrentGoal++;
        Debug.Log($"[GameDayManager] Wave completed! Total: {wavesCompletedForCurrentGoal}/{wavesBetweenGoals}, PlayerHasFootball: {playerHasFootball}");

        // Check if we should spawn a goal post
        if (wavesCompletedForCurrentGoal >= wavesBetweenGoals && playerHasFootball)
        {
            Debug.Log("[GameDayManager] Conditions met! Spawning goal post...");
            SpawnGoalPost();
            wavesCompletedForCurrentGoal = 0;
            wavesBetweenGoals = UnityEngine.Random.Range(3, 6);
        }
        else if (wavesCompletedForCurrentGoal >= wavesBetweenGoals && !playerHasFootball)
        {
            Debug.Log("[GameDayManager] Wave requirement met but player doesn't have football yet!");
        }
    }

    private void SpawnGoalPost()
    {
        if (spawner == null)
        {
            Debug.LogWarning("[GameDayManager] Spawner not found!");
            return;
        }

        GameObject goalPostPrefab = null;
        GameManager gm = FindObjectOfType<GameManager>();

        if (gm != null && gm.CurrentGameDayDifficulty == GameDayDifficulty.College)
        {
            goalPostPrefab = spawner.goalPostEasyPrefab;
            Debug.Log("[GameDayManager] Using College (Easy) goal post");
        }
        else if (gm != null && gm.CurrentGameDayDifficulty == GameDayDifficulty.Pro)
        {
            goalPostPrefab = spawner.goalPostProPrefab;
            Debug.Log("[GameDayManager] Using Pro goal post");
        }

        if (goalPostPrefab == null)
        {
            Debug.LogWarning("[GameDayManager] Goal post prefab not assigned in Spawner!");
            return;
        }

        float randomY = UnityEngine.Random.Range(-1f, 2f);
        Vector3 spawnPos = new Vector3(goalPostSpawnX, randomY, 0f);

        Instantiate(goalPostPrefab, spawnPos, Quaternion.identity);
        Debug.Log($"[GameDayManager] Goal post spawned at {spawnPos}!");
    }

    public bool IsInDefenseRound()
    {
        return inDefenseRound;
    }

    public bool IsPlayerCarryingFootball()
    {
        return playerHasFootball;
    }

    public bool IsSpawningPaused()
    {
        return isSpawningPaused;
    }

    public bool IsBallCarrierSpawningThisFrame()
    {
        // Check if ball carrier is about to spawn right now
        if (inDefenseRound && !ballCarrierSpawnedThisDefense)
        {
            float elapsedTime = defenseRoundTimeout - defenseRoundTimer;
            return elapsedTime >= ballCarrierSpawnDelay && elapsedTime < (ballCarrierSpawnDelay + Time.deltaTime + 0.1f);
        }
        return false;
    }

    public void ResetGameDayOnGameOver()
    {
        // Reset all Game Day state when game ends
        inDefenseRound = false;
        playerHasFootball = false;
        wavesCompletedForCurrentGoal = 0;
        wavesBetweenGoals = 10;
        defenseRoundTimer = 0f;
        ballCarrierActive = false;
        ballCarrierSpawnedThisDefense = false;
        isSpawningPaused = false;
        
        UpdateModeDisplay(); // This will show "OFFENSE" since inDefenseRound is false
        Debug.Log("[GameDayManager] Game Day state reset to Offense mode");
    }

    private void SpawnBallCarrierAfterDelay()
    {
        if (spawner == null)
        {
            Debug.LogWarning("[GameDayManager] Spawner not found!");
            return;
        }

        spawner.SpawnBallCarrierAtScreenCenter();
        Debug.Log("[GameDayManager] Ball carrier spawned after 10 seconds of defense!");
    }
}