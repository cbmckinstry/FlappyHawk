using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.IO;
using UnityEngine.InputSystem;

public enum Difficulty { Easy = 0, Normal = 1, Hard = 2 }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public Player player;
    public TextMeshProUGUI scoreText;
    public GameObject playButton;
    public GameObject gameOver;
    public GameObject readyButton;

    [Header("Difficulty UI")]
    public Image difficultyImage;
    public Sprite easySprite;
    public Sprite normalSprite;
    public Sprite hardSprite;

    private int score;
    private Difficulty currentDifficulty = Difficulty.Easy;
    public Difficulty CurrentDifficulty => currentDifficulty;

    public static event Action<float> OnPipeSpeedChanged;
    public float CurrentPipeSpeed { get; private set; } 

    private float pipeSpeed = 5f;
    private float easySpawnRate = 1.15f;
    private float normalSpawnRate = 1f;
    private float hardSpawnRate = 0.85f;

    public static event Action<float> OnSpawnRateChanged;
    
    private float easyGravity = -9.8f;
    private float normalGravity = -9.8f;
    private float hardGravity = -9.8f;

    private DateTime roundStartUtc;
    private float roundElapsed; 
    private int pipesSpawnedThisRound;
    private int jumpsThisRound;

    public void IncreaseScore()
    {
        score++;
        scoreText.text = score.ToString();
    }

    public void Awake() { 
        Instance = this;
        Application.targetFrameRate = 60; 
        gameOver.SetActive(false);
        Pause(); 
        ApplyDifficulty(); 
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

    foreach (var p in FindObjectsOfType<Pipes>())
        Destroy(p.gameObject);
}

public void GameOver()
{
    gameOver.SetActive(true);
    playButton.SetActive(true);
    readyButton.SetActive(false);
    if (difficultyImage != null) difficultyImage.gameObject.SetActive(true);
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
        player.enabled = false;
    }


    public void ChangeDifficulty()
    {
        currentDifficulty = (Difficulty)(((int)currentDifficulty + 1) % 3);
        ApplyDifficulty();
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

    if (difficultyImage != null)
        difficultyImage.sprite = currentSprite;
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


}
