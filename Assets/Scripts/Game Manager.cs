using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.IO;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum Difficulty { Easy = 0, Normal = 1, Hard = 2 }

public class GameManager : MonoBehaviour
{
    // Set by MainMenu before loading the Game scene.
    public static Difficulty StartDifficulty = Difficulty.Easy;

    public static GameManager Instance { get; private set; }

    [Header("Scene Refs")]
    public Player player;
    public TextMeshProUGUI scoreText;
    public GameObject gameOver;
    public GameObject getReady;
    public GameObject menuButton;

    [Header("Difficulty UI")]
    public Image difficultyImage;
    public Sprite easySprite;
    public Sprite normalSprite;
    public Sprite hardSprite;

    [Header("Tuning")]
    [SerializeField] private float pipeSpeed = 5f;
    [SerializeField] private float easySpawnRate   = 1.15f;
    [SerializeField] private float normalSpawnRate = 1.00f;
    [SerializeField] private float hardSpawnRate   = 0.85f;

    [SerializeField] private float easyGravity   = -9.8f;
    [SerializeField] private float normalGravity = -9.8f;
    [SerializeField] private float hardGravity   = -9.8f;

    // Broadcasts so Spawner/Pipes can react in real-time if needed.
    public static event Action<float> OnSpawnRateChanged;
    public static event Action<float> OnPipeSpeedChanged;

    public float CurrentPipeSpeed { get; private set; }
    public float CurrentSpawnRate { get; private set; }

    private int score;
    private Difficulty currentDifficulty = Difficulty.Easy;
    public Difficulty CurrentDifficulty => currentDifficulty;

    private DateTime roundStartUtc;
    private float roundElapsed;
    private int pipesSpawnedThisRound;
    private int jumpsThisRound;

    private void Awake()
    {
        Instance = this;
        Application.targetFrameRate = 60;

        if (gameOver)    gameOver.SetActive(false);
        if (menuButton) menuButton.SetActive(true);
        if (getReady) getReady.SetActive(true);
        if (difficultyImage) difficultyImage.gameObject.SetActive(true);

        currentDifficulty = StartDifficulty;

        Pause();
        ApplyDifficulty();

        score = 0;
        if (scoreText) scoreText.text = "0";
    }

    private void Update()
    {
        bool jumpPressed =
            (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
            (Gamepad.current  != null && Gamepad.current.buttonSouth.wasPressedThisFrame);

        if (jumpPressed && !player.enabled && Time.timeScale == 0f)
        {
            Play();
            return;
        }
        if (player != null && player.enabled && Time.timeScale > 0f)
        {
            roundElapsed += Time.unscaledDeltaTime;
            if (jumpPressed) jumpsThisRound++;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            QuitGame();
    }

    public void Play()
    {
        pipesSpawnedThisRound = 0;
        jumpsThisRound = 0;
        roundElapsed = 0f;
        roundStartUtc = DateTime.UtcNow;

        score = 0;
        if (scoreText) scoreText.text = "0";
        if (gameOver) gameOver.SetActive(false);
        if (getReady) getReady.SetActive(false);
        if (menuButton) menuButton.SetActive(false);
        if (difficultyImage) difficultyImage.gameObject.SetActive(false);

        foreach (var p in FindObjectsOfType<Pipes>())
            Destroy(p.gameObject);

        Time.timeScale = 1f;
        if (player) player.enabled = true;
    }

    public void Menu(){
        SceneManager.LoadScene("MenuScreen");
    }

    public void GameOver()
    {
        if (gameOver) gameOver.SetActive(true);
        if (menuButton) menuButton.SetActive(true);
        if (difficultyImage) difficultyImage.gameObject.SetActive(true);

        Pause();

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
        if (player) player.enabled = false;
    }

    public void SetDifficulty(Difficulty d)
    {
        currentDifficulty = d;
        StartDifficulty   = d;
        ApplyDifficulty();
    }

    private void ApplyDifficulty()
    {
        float spawnRate;
        Sprite spriteToShow;

        switch (currentDifficulty)
        {
            case Difficulty.Normal:
                spawnRate = normalSpawnRate;
                spriteToShow = normalSprite;
                if (player) player.gravity = normalGravity;
                break;

            case Difficulty.Hard:
                spawnRate = hardSpawnRate;
                spriteToShow = hardSprite;
                if (player) player.gravity = hardGravity;
                break;

            default: // Easy
                spawnRate = easySpawnRate;
                spriteToShow = easySprite;
                if (player) player.gravity = easyGravity;
                break;
        }

        CurrentPipeSpeed = pipeSpeed;
        foreach (var p in FindObjectsOfType<Pipes>())
            p.pipeSpeed = pipeSpeed;
        OnPipeSpeedChanged?.Invoke(pipeSpeed);

        CurrentSpawnRate = spawnRate;
        OnSpawnRateChanged?.Invoke(spawnRate);

        if (difficultyImage) difficultyImage.sprite = spriteToShow;
    }

    public void IncreaseScore()
    {
        score++;
        if (scoreText) scoreText.text = score.ToString();
    }

    public void RegisterJump() { jumpsThisRound++; }
    public void RegisterPipe() { pipesSpawnedThisRound++; }

    public void QuitGame()
    {
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }
}
