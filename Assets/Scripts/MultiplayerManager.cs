using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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

    [Header("Ready Up UI")]
    public GameObject readyMenu;
    public TextMeshProUGUI p1Text;
    public TextMeshProUGUI p2Text;

    [Header("Player Labels")]
    public RectTransform player1Label;
    public RectTransform player2Label;

    // Ready state
    private bool p1Ready = false;
    private bool p2Ready = false;

    // Controller assignments
    private Gamepad p1Controller;
    private Gamepad p2Controller;

    private Camera cam;

    // === STATE ===
    public bool InDefenseRound { get; private set; } = false;
    private bool isSpawningPaused = false;
    private bool defenseCarrierTackled = false;

    private Player ballCarrier;
    private Player blocker;

    private int teamScore = 0;
    private int opponentScore = 0;

    private Coroutine modePopupRoutine;

    // Offense drive counter (1,2,3,4,...)
    private int offenseRoundNumber = 0;

    private enum ModeDisplayType
    {
        Offense,
        Defense
    }

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

        cam = Camera.main;

        // Hide gameplay UI until ready
        playButton?.SetActive(false);
        readyMenu?.SetActive(true);

        // Hide birds until joined
        if (player1 != null) player1.gameObject.SetActive(false);
        if (player2 != null) player2.gameObject.SetActive(false);

        // Default text
        p1Text.text = "Player 1: A / X / W Key to connect";
        p2Text.text = "Player 2: A / X / Up Key to connect";

        Pause();
    }

    // ============================================================
    //  ROLE SETUP
    // ============================================================

    private void SetInitialRoles()
    {
        if (player1 == null || player2 == null) return;

        // Just set IDs and multiplayer flags here.
        // Actual carrier/blocker assignment is done in StartOffenseRound.
        player1.playerID = Player.PlayerID.Player1;
        player2.playerID = Player.PlayerID.Player2;

        player1.isMultiplayer = true;
        player2.isMultiplayer = true;
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

    private void UpdateModeDisplay(ModeDisplayType mode, bool playPopup = true)
    {
        if (modeText == null) return;

        switch (mode)
        {
            case ModeDisplayType.Offense:
                modeText.text = "OFFENSE";
                break;
            case ModeDisplayType.Defense:
                modeText.text = "DEFENSE";
                break;
        }

        if (playPopup)
            ShowModePopup();
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

    private void Update()
    {
        if (!PauseManager.GameIsActive)
        {
            if (!p1Ready || !p2Ready)
            {
                CheckForControllerJoin();
            }
            else
            {
                if (playButton.activeSelf)
                {
                    foreach (var pad in Gamepad.all)
                    {
                        if (pad.buttonSouth.wasPressedThisFrame)
                        {
                            playButton.GetComponent<Button>().onClick.Invoke();
                            return;
                        }
                    }
                }
            }
        }
        else
        {
            if (playButton.activeSelf)
            {
                // Controllers can start
                foreach (var pad in Gamepad.all)
                {
                    if (pad.buttonSouth.wasPressedThisFrame)
                    {
                        playButton.GetComponent<Button>().onClick.Invoke();
                        return;
                    }
                }

                // Keyboard can start (Enter or Space)
                if (Keyboard.current != null &&
                    (Keyboard.current.enterKey.wasPressedThisFrame ||
                     Keyboard.current.spaceKey.wasPressedThisFrame))
                {
                    playButton.GetComponent<Button>().onClick.Invoke();
                    return;
                }
            }
        }

        UpdatePlayerLabels();
    }

    private void CheckForControllerJoin()
    {
        // ============================
        // CONTROLLER JOIN
        // ============================
        var pads = Gamepad.all;

        foreach (var pad in pads)
        {
            // Player 1 join (controller)
            if (!p1Ready && pad.buttonSouth.wasPressedThisFrame)
            {
                p1Ready = true;
                p1Controller = pad;

                ActivatePlayerSlot(1);
                p1Text.text = "Player 1: Controller connected";
                continue;
            }

            // Player 2 join (controller)
            if (!p2Ready &&
                pad.buttonSouth.wasPressedThisFrame &&
                p1Controller != null &&
                pad.deviceId != p1Controller.deviceId)
            {
                p2Ready = true;
                p2Controller = pad;

                ActivatePlayerSlot(2);
                p2Text.text = "Player 2: Controller connected";
                continue;
            }
        }

        // ============================
        // KEYBOARD JOIN
        // ============================
        if (Keyboard.current != null)
        {
            // Player 1 join (keyboard)
            if (!p1Ready && Keyboard.current.wKey.wasPressedThisFrame)
            {
                p1Ready = true;
                p1Controller = null;   // keyboard-controlled
                ActivatePlayerSlot(1);
                p1Text.text = "Player 1: Keyboard (W/S)";
            }

            // Player 2 join (keyboard)
            if (!p2Ready && Keyboard.current.upArrowKey.wasPressedThisFrame)
            {
                p2Ready = true;
                p2Controller = null;   // keyboard-controlled
                ActivatePlayerSlot(2);
                p2Text.text = "Player 2: Keyboard (Up/Down)";
            }
        }

        // ============================
        // ENABLE PLAY BUTTON
        // ============================
        if (p1Ready && p2Ready)
        {
            playButton?.SetActive(true);
            SelectPlayButton();
        }
    }

    private void ActivatePlayerSlot(int slot)
    {
        if (slot == 1)
        {
            player1.gameObject.SetActive(true);
            player1.playerID = Player.PlayerID.Player1;
            player1.isMultiplayer = true;

            // Position
            player1.transform.position = new Vector3(-1.5f, 0f, 0f);
        }
        else
        {
            player2.gameObject.SetActive(true);
            player2.playerID = Player.PlayerID.Player2;
            player2.isMultiplayer = true;

            player2.transform.position = new Vector3(1.5f, 0f, 0f);
        }
    }

    private void UpdatePlayerLabels()
    {
        if (cam == null) return;

        if (player1 != null && player1.gameObject.activeSelf)
        {
            Vector3 pos = cam.WorldToScreenPoint(player1.transform.position + new Vector3(0, 1f, 0));
            player1Label.position = pos;
        }

        if (player2 != null && player2.gameObject.activeSelf)
        {
            Vector3 pos = cam.WorldToScreenPoint(player2.transform.position + new Vector3(0, 1f, 0));
            player2Label.position = pos;
        }
    }

    public Gamepad GetControllerForPlayer(Player.PlayerID pid)
    {
        if (pid == Player.PlayerID.Player1) return p1Controller;
        return p2Controller;
    }

    // ============================================================
    //  PLAY / RESET / GAME OVER
    // ============================================================

    public void Play()
    {
        PauseManager.GameIsActive = true;

        readyMenu?.SetActive(false);
        playButton?.SetActive(false);
        readyButton?.SetActive(false);
        gameOverPanel?.SetActive(false);

        ActivatePlayer(player1, -1.5f);
        ActivatePlayer(player2, 1.5f);

        // Re-enable player tags
        if (player1Label != null)
            player1Label.gameObject.SetActive(true);
        if (player2Label != null)
            player2Label.gameObject.SetActive(true);

        ResetScores();
        spawner?.ResetSpawner();

        SetInitialRoles();
        offenseRoundNumber = 0;      // reset counter at the start of a match
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
        // If we already successfully tackled the carrier in this defense round,
        // ignore any late GameOver calls coming from leftover collisions.
        if (InDefenseRound && defenseCarrierTackled)
            return;

        AudioManager.Instance?.PlaySplat();

        PauseManager.GameIsActive = false;

        // Hide players
        if (player1 != null) player1.gameObject.SetActive(false);
        if (player2 != null) player2.gameObject.SetActive(false);

        // Despawn football, goal posts, and all spawned enemies
        if (spawner != null)
            spawner.ResetSpawner();

        // Hide player tags (labels above heads)
        if (player1Label != null)
            player1Label.gameObject.SetActive(false);
        if (player2Label != null)
            player2Label.gameObject.SetActive(false);

        // Update game over scores
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
        AudioManager.Instance?.PlayWhistle();

        InDefenseRound = false;
        isSpawningPaused = false;

        // Increment offense round number
        offenseRoundNumber++;

        // Decide ball carrier / blocker based on odd/even offense number
        if (player1 != null && player2 != null)
        {
            if (offenseRoundNumber % 2 == 1)
            {
                // Odd offense drives: P1 carries, P2 blocks
                ballCarrier = player1;
                blocker = player2;
            }
            else
            {
                // Even offense drives: P2 carries, P1 blocks
                ballCarrier = player2;
                blocker = player1;
            }
        }

        GiveHelmetsForOffense();
        UpdateModeDisplay(ModeDisplayType.Offense, true);

        spawner?.ResetSpawner();
        PositionPlayersForNewDrive();

        // Give ball to current carrier
        spawner?.SpawnFootball(ballCarrier);
    }

    private void StartDefenseRound()
    {
        AudioManager.Instance?.PlayWhistle();

        InDefenseRound = true;
        defenseCarrierTackled = false;
        isSpawningPaused = false;

        GiveHelmetsForDefense();
        UpdateModeDisplay(ModeDisplayType.Defense, true);

        spawner?.ResetSpawner();
        StartCoroutine(DefenseRoundTimer());
    }

    public void OnDefenseCarrierTackled()
    {
        if (!InDefenseRound)
            return;

        defenseCarrierTackled = true;

        // Despawn ALL spawned objects (birds, posts, football, etc.)
        if (spawner != null)
            spawner.ResetSpawner();

        // End defense round as a player win
        EndDefenseRound(true);
    }

    private IEnumerator DefenseRoundTimer()
    {
        yield return new WaitForSeconds(defenseRoundDuration);
        if (InDefenseRound)
            EndDefenseRound(false);
    }

    public void EndDefenseRound(bool playerWon)
    {
        // Kill all relevant spawned objects
        foreach (var obj in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            if (obj is CycloneBird or Football or GoalPost or BallCarrierBird)
                Destroy(obj.gameObject);

        InDefenseRound = false;
        isSpawningPaused = false;

        if (!playerWon)
        {
            // enemy scores 3 or 7
            int pts = Random.value < 0.7f ? 3 : 7;
            opponentScore += pts;
            UpdateScoreUI();
        }

        // Go straight into next offense round; it will alternate carrier by odd/even
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
