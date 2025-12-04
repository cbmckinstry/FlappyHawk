using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MultiplayerManager : MonoBehaviour
{
    public static MultiplayerManager Instance { get; private set; }

    [Header("Players")]
    public Player player1;
    public Player player2;

    [Header("UI")]
    public GameObject playButton;
    public GameObject gameOverPanel;
    public GameObject readyButton;

    public TextMeshProUGUI modeText;
    public TextMeshProUGUI teamScoreText;
    public TextMeshProUGUI opponentScoreText;
    public TextMeshProUGUI goTeamScoreText;
    public TextMeshProUGUI goOpponentScoreText;

    [Header("Spawner")]
    public MultiplayerSpawner spawner;

    [Header("Round Settings")]
    [SerializeField] private float defenseRoundDuration = 10f;

    // === STATE ===
    public bool InDefenseRound { get; private set; } = false;
    private bool isSpawningPaused = false;

    private Player ballCarrier;
    private Player blocker;

    private int teamScore = 0;
    private int opponentScore = 0;

    private Coroutine modePopupRoutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        gameOverPanel?.SetActive(false);

        if (player1 != null) player1.gameObject.SetActive(false);
        if (player2 != null) player2.gameObject.SetActive(false);

        Pause();
    }

    private void Start()
    {
        ControllerInputManager.Instance.RecheckControllers();
        SelectPlayButton();
        ResetScores();
        SetInitialRoles();
        UpdateScoreUI();
        UpdateModeDisplay(false);
    }

    // ============================================================
    //  ROLE SETUP
    // ============================================================

    private void SetInitialRoles()
    {
        if (player1 == null || player2 == null) return;

        ballCarrier = player1; // P1 always starts with ball
        blocker = player2;

        player1.playerID = Player.PlayerID.Player1;
        player2.playerID = Player.PlayerID.Player2;

        player1.isMultiplayer = true;
        player2.isMultiplayer = true;
    }

    private void SwapRoles()
    {
        var tmp = ballCarrier;
        ballCarrier = blocker;
        blocker = tmp;

        PositionPlayersForNewDrive();
    }

    private void PositionPlayersForNewDrive()
    {
        if (ballCarrier != null)
        {
            Vector3 bcPos = ballCarrier.transform.position;
            bcPos.x = -1.5f;
            bcPos.y = 0f;
            ballCarrier.transform.position = bcPos;
        }

        if (blocker != null)
        {
            Vector3 blPos = blocker.transform.position;
            blPos.x = 1.5f;
            blPos.y = 0f;
            blocker.transform.position = blPos;
        }
    }

    public bool IsBallCarrier(Player p) => p == ballCarrier;

    public Player GetBallCarrier() => ballCarrier;

    // ============================================================
    //  UI HELPERS
    // ============================================================

    private void SelectPlayButton()
    {
        if (playButton != null)
        {
            Button b = playButton.GetComponent<Button>();
            if (b != null)
                EventSystem.current?.SetSelectedGameObject(b.gameObject);
        }
    }

    private void ResetScores()
    {
        teamScore = 0;
        opponentScore = 0;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (teamScoreText != null)
            teamScoreText.text = teamScore.ToString();

        if (opponentScoreText != null)
            opponentScoreText.text = opponentScore.ToString();
    }

    private void UpdateModeDisplay(bool playPopup = true)
    {
        if (modeText == null) return;

        modeText.text = InDefenseRound ? "DEFENSE" : "OFFENSE";
        if (playPopup) ShowModePopup();
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

    // ============================================================
    //  PLAY / RESET / GAME OVER
    // ============================================================

    public void Play()
    {
        PauseManager.GameIsActive = true;

        playButton?.SetActive(false);
        readyButton?.SetActive(false);
        gameOverPanel?.SetActive(false);

        ActivatePlayer(player1, -1.5f);
        ActivatePlayer(player2, 1.5f);

        ResetScores();
        spawner?.ResetSpawner();

        SetInitialRoles();
        PositionPlayersForNewDrive();
        StartOffenseRound();

        Time.timeScale = 1f;
    }

    private void ActivatePlayer(Player p, float xPos)
    {
        if (p == null) return;

        p.gameObject.SetActive(true);
        p.enabled = true;

        Vector3 pos = p.transform.position;
        pos.x = xPos;
        pos.y = 0f;
        p.transform.position = pos;
    }

    public void Pause()
    {
        Time.timeScale = 0f;
        if (player1 != null) player1.enabled = false;
        if (player2 != null) player2.enabled = false;
        isSpawningPaused = true;
    }

    public void GameOver()
    {
        PauseManager.GameIsActive = false;

        spawner?.ResetSpawner();

        if (player1 != null) player1.gameObject.SetActive(false);
        if (player2 != null) player2.gameObject.SetActive(false);

        if (goTeamScoreText != null)
            goTeamScoreText.text = teamScore.ToString();

        if (goOpponentScoreText != null)
            goOpponentScoreText.text = opponentScore.ToString();

        gameOverPanel?.SetActive(true);
        playButton?.SetActive(true);

        Pause();
        SelectPlayButton();
    }

    // ============================================================
    //  ROUND FLOW (OFFENSE / DEFENSE)
    // ============================================================

    private void StartOffenseRound()
    {
        InDefenseRound = false;
        isSpawningPaused = false;

        GiveHelmetsForOffense();
        UpdateModeDisplay(true);

        spawner?.ResetSpawner();
        PositionPlayersForNewDrive();

        // Give ball to current carrier
        spawner?.SpawnFootball(ballCarrier);
    }

    private void StartDefenseRound()
    {
        InDefenseRound = true;
        isSpawningPaused = false;

        GiveHelmetsForDefense();
        UpdateModeDisplay(true);

        spawner?.ResetSpawner();
        StartCoroutine(DefenseRoundTimer());
    }

    private IEnumerator DefenseRoundTimer()
    {
        yield return new WaitForSeconds(defenseRoundDuration);
        if (InDefenseRound)
            EndDefenseRound(false);
    }

    public void EndDefenseRound(bool playerWon)
    {
        InDefenseRound = false;
        isSpawningPaused = false;

        UpdateModeDisplay(true);

        if (!playerWon)
        {
            // enemy scores 3 or 7
            int pts = Random.value < 0.7f ? 3 : 7;
            opponentScore += pts;
            UpdateScoreUI();
        }

        // swap roles AFTER defense
        SwapRoles();

        // restart offense with new ball carrier
        StartOffenseRound();
    }

    public void OnEnemyBallCarrierDespawned()
    {
        if (InDefenseRound)
            EndDefenseRound(false);
    }

    // ============================================================
    //  SCORING
    // ============================================================

    public void OnPlayerEnteredScoring(Player p)
    {
        if (!IsBallCarrier(p) || InDefenseRound)
            return;

        teamScore += 7;
        UpdateScoreUI();

        StartDefenseRound();
    }

    public void OnBallDroppedScored(Player p)
    {
        if (!IsBallCarrier(p) || InDefenseRound)
            return;

        teamScore += 3;
        UpdateScoreUI();

        StartDefenseRound();
    }

    // ============================================================
    //  FOOTBALL DROP (S / DOWN ARROW)
    // ============================================================

    public void HandleFootballDrop(Player p)
    {
        if (!IsBallCarrier(p) || InDefenseRound) return;

        MultiplayerFootball fb = FindFirstObjectByType<MultiplayerFootball>();
        if (fb == null) return;

        fb.Drop();
    }

    // ============================================================
    //  HELMETS
    // ============================================================

    private void GiveHelmetsForOffense()
    {
        // Carrier: no helmet on offense
        ballCarrier?.SetMultiplayerHelmet(false);

        // Blocker: infinite helmet
        blocker?.SetMultiplayerHelmet(true);
    }

    private void GiveHelmetsForDefense()
    {
        player1?.SetMultiplayerHelmet(true);
        player2?.SetMultiplayerHelmet(true);
    }
    
    public void TriggerDefenseRound()
    {
        StartDefenseRound();
    }

}
