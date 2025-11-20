using UnityEngine;
using System;
using UnityEngine.SceneManagement;

/// <summary>
/// Central compatibility hub that bridges old code (which references GameManager)
/// with the new split-system (IowaManager and GameDayManager).
/// Keeps both scenes functional without needing rewrites.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // === Bridge Instances ===
    public static IowaManager IowaInstance => IowaManager.Instance;
    public static GameDayManager GameDayInstance => GameDayManager.Instance;

    // ============================= ENUMS =============================
    public enum Difficulty { Easy, Normal, Hard }
    public enum GameDayDifficulty { College, Pro }
    public enum GameMode { Iowa, GameDay }

    // ============================= EVENTS =============================
    public static event Action<float> OnScrollSpeedChanged
    {
        add
        {
            if (IowaInstance != null)
                IowaManager.OnScrollSpeedChanged += value;
            else if (GameDayInstance != null)
                GameDayManager.OnScrollSpeedChanged += value;
        }
        remove
        {
            if (IowaInstance != null)
                IowaManager.OnScrollSpeedChanged -= value;
            else if (GameDayInstance != null)
                GameDayManager.OnScrollSpeedChanged -= value;
        }
    }

    public static event Action<float> OnSpawnRateChanged
    {
        add
        {
            if (IowaInstance != null)
                IowaManager.OnSpawnRateChanged += value;
            else if (GameDayInstance != null)
                GameDayManager.OnSpawnRateChanged += value;
        }
        remove
        {
            if (IowaInstance != null)
                IowaManager.OnSpawnRateChanged -= value;
            else if (GameDayInstance != null)
                GameDayManager.OnSpawnRateChanged -= value;
        }
    }

    // ============================= PROPERTIES =============================
    public static float CurrentScrollSpeed =>
        IowaInstance != null ? IowaInstance.CurrentScrollSpeed :
        GameDayInstance != null ? GameDayInstance.CurrentScrollSpeed : 5f;

    public static float CurrentSpawnRate =>
        IowaInstance != null ? IowaInstance.CurrentSpawnRate :
        GameDayInstance != null ? GameDayInstance.CurrentSpawnRate : 1f;

    public static Difficulty StartDifficulty
    {
        get => IowaManager.StartDifficulty;
        set => IowaManager.StartDifficulty = value;
    }

    public static GameMode CurrentGameMode =>
        GameDayInstance != null ? GameMode.GameDay : GameMode.Iowa;

    public static Difficulty CurrentDifficulty =>
        IowaInstance != null ? IowaInstance.CurrentDifficulty : Difficulty.Easy;

    // ============================= LIFECYCLE =============================
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ============================= LEGACY HELPERS =============================
    public static void IncreaseScore(int amount = 1)
    {
        IowaInstance?.IncreaseScore(amount);
        GameDayInstance?.IncreaseScore(amount);
    }

    public static void IncreaseOpponentScore(int amount = 1)
    {
        GameDayInstance?.IncreaseOpponentScore(amount);
    }

    public static void RegisterObstacle() => IowaInstance?.RegisterObstacle();

    public static void GameOver()
    {
        if (IowaInstance != null)
            IowaInstance.GameOver();
        else if (GameDayInstance != null)
            GameDayInstance.GameOver();
    }

    public static void OnPlayerDamaged(int helmetDurability)
    {
        IowaInstance?.OnPlayerDamaged(helmetDurability);
        GameDayInstance?.OnPlayerDamaged(helmetDurability);
    }

    public static void OnPlayerHealed(int helmetDurability)
    {
        IowaInstance?.OnPlayerHealed(helmetDurability);
        GameDayInstance?.OnPlayerHealed(helmetDurability);
    }

    public static bool IsGameActive()
    {
        if (IowaInstance != null)
            return IowaInstance.IsGameActive();

        if (GameDayInstance != null)
            return GameDayInstance.IsGameActive();

        return false;
    }

}