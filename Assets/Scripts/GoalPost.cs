using UnityEngine;

public class GoalPost : MonoBehaviour
{
    [Header("Goal Post Settings")]
    public float moveSpeed = 4.5f;
    private float leftEdge;
    private bool hasScored = false;

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
        gameObject.tag = "GoalPost";
    }

    private void Update()
    {
        // Move left with the rest of the scene
        transform.position += Vector3.left * moveSpeed * Time.deltaTime;

        // Destroy if off-screen
        if (transform.position.x < leftEdge)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasScored) return;

        Player player = other.GetComponent<Player>();
        EnemyBallCarrier enemyCarrier = other.GetComponent<EnemyBallCarrier>();
        Football football = other.GetComponent<Football>();

        // Case 1: Player passes through with football = 7 points (Hawkeye/Player scores)
        if (player != null)
        {
            Football carriedBall = FindFirstObjectByType<Football>();
            if (carriedBall != null && carriedBall.IsCarried())
            {
                GameManager.IncreaseScore(7);
                Destroy(carriedBall.gameObject);
                hasScored = true;
                TriggerDefenseRound();
                return;
            }
        }

        // Case 2: Enemy passes through with football = 7 points (Cyclone/Opponent scores)
        if (enemyCarrier != null)
        {
            Football carriedBall = FindFirstObjectByType<Football>();
            if (carriedBall != null && carriedBall.IsCarried())
            {
                GameManager.IncreaseOpponentScore(7);
                Destroy(carriedBall.gameObject);
                hasScored = true;
                TriggerDefenseRound();
                return;
            }
        }

        // Case 3: Dropped football goes through = 3 points (Cyclone/Opponent scores)
        if (football != null && !football.IsCarried())
        {
            GameManager.IncreaseOpponentScore(3);
            Destroy(football.gameObject);
            hasScored = true;
            TriggerDefenseRound();
        }
    }

    private void TriggerDefenseRound()
    {
        GameDayManager gameDayMgr = FindFirstObjectByType<GameDayManager>();
        if (gameDayMgr != null)
        {
            gameDayMgr.StartDefenseRound();
        }

        // Slight delay before destroying for smoother transitions
        Invoke(nameof(DestroySelf), 0.1f);
    }

    private void DestroySelf()
    {
        Destroy(gameObject);
    }
}
