using UnityEngine;

public class GoalPost : MonoBehaviour
{
    [Header("Goal Post Settings")]
    public float moveSpeed = 4.5f;
    private float leftEdge;
    private bool hasScored = false;   // prevents double-processing

    private void OnEnable()
    {
        // Set initial speed from global GameManager
        moveSpeed = GameManager.CurrentScrollSpeed;

        // Subscribe for updates when scroll speed changes
        GameManager.OnScrollSpeedChanged += HandleSpeedChanged;
    }

    private void OnDisable()
    {
        GameManager.OnScrollSpeedChanged -= HandleSpeedChanged;
    }

    private void HandleSpeedChanged(float newSpeed)
    {
        moveSpeed = newSpeed;
    }

    private void Start()
    {
        if (Camera.main == null)
        {
            Debug.LogError("No Main Camera found in scene!");
            return;
        }

        leftEdge = Camera.main.ScreenToWorldPoint(Vector3.zero).x - 1f;

        // Tag so collisions are clear in editor (not strictly required)
        gameObject.tag = "GoalPost";
    }

    private void Update()
    {
        // Move left with the rest of the scene
        transform.position += Vector3.left * moveSpeed * Time.deltaTime;

        // Case 4: Missed field goal = post goes off-screen left → start defense
        if (!hasScored && transform.position.x < leftEdge)
        {
            TriggerDefenseRound();
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasScored) return;

        // Try to get current GameDay manager
        GameDayManager gameDayMgr = FindFirstObjectByType<GameDayManager>();

        Player player = other.GetComponent<Player>();
        Football football = other.GetComponent<Football>();

        // ------------------------------------------------
        // Case 1: Player passes through WITH carried ball
        // ------------------------------------------------
        if (player != null)
        {
            // Look up the single-player football and check if it's being carried
            Football carriedBall = FindFirstObjectByType<Football>();

            if (carriedBall != null && carriedBall.IsCarried())
            {
                // Touchdown: +7 to *GameDay* player score
                AudioManager.Instance?.PlayTouchdown();
                if (gameDayMgr != null)
                {
                    gameDayMgr.IncreaseScore(7);
                }

                Destroy(carriedBall.gameObject);

                hasScored = true;

                // Drive ends → switch to defense
                TriggerDefenseRound();
                Destroy(gameObject);
                return;
            }
            else
            {
                // ------------------------------------------------
                // Case 3: Player through post WITHOUT the ball
                //        (turnover, no points, go to defense)
                // ------------------------------------------------
                hasScored = true;
                TriggerDefenseRound();
                Destroy(gameObject);
                return;
            }
        }

        // ------------------------------------------------
        // Case 2: Dropped football goes through
        //         (field goal: +3 points)
        // ------------------------------------------------
        if (football != null && !football.IsCarried())
        {
            AudioManager.Instance?.PlayFieldGoal();

            if (gameDayMgr != null)
            {
                gameDayMgr.IncreaseScore(3);
            }

            Destroy(football.gameObject);

            hasScored = true;

            // Drive ends → switch to defense
            TriggerDefenseRound();
            Destroy(gameObject);
        }
    }

    private void TriggerDefenseRound()
    {
        GameDayManager gameDayMgr = FindFirstObjectByType<GameDayManager>();
        if (gameDayMgr != null)
        {
            gameDayMgr.StartDefenseRound();
        }
    }
}
