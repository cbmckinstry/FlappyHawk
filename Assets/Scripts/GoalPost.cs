using UnityEngine;

public class GoalPost : MonoBehaviour
{
    [Header("Goal Post Settings")]
    public float moveSpeed = 4.5f;
    private float leftEdge;
    private bool hasScored = false;
    private Football ballInGoal;

    private void OnEnable()
    {
        // Subscribe to pipe speed changes for consistency
        var gm = FindObjectOfType<GameManager>();
        if (gm != null) moveSpeed = gm.CurrentPipeSpeed;
        GameManager.OnPipeSpeedChanged += HandleSpeedChanged;
    }

    private void OnDisable()
    {
        GameManager.OnPipeSpeedChanged -= HandleSpeedChanged;
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
        // Don't tag as "Scoring" - handles its own scoring in Game Day mode
    }

    private void Update()
    {
        // Move left with the game
        transform.position += Vector3.left * moveSpeed * Time.deltaTime;
        
        // Destroy when off screen
        if (transform.position.x < leftEdge)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasScored) return;

        // Check if player with football passes through
        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            // Find football and check if player is carrying it
            Football football = FindObjectOfType<Football>();
            if (football != null && football.IsCarried())
            {
                // Flying through goal post with football = 7 points
                FindObjectOfType<GameManager>().IncreaseScore(7);
                Destroy(football.gameObject);
                hasScored = true;
                TriggerDefenseRound();
                return;
            }
            // Player without football doesn't score - just return
            return;
        }

        // Check if football is dropped into goal post
        Football ball = other.GetComponent<Football>();
        if (ball != null && !ball.IsCarried())
        {
            // Dropped into goal post = 3 points
            FindObjectOfType<GameManager>().IncreaseScore(3);
            Destroy(ball.gameObject);
            hasScored = true;
            TriggerDefenseRound();
            return;
        }
    }

    private void TriggerDefenseRound()
    {
        // Notify GameDayManager to start defense round
        GameDayManager gameDayMgr = FindObjectOfType<GameDayManager>();
        if (gameDayMgr != null)
        {
            gameDayMgr.StartDefenseRound();
        }
        
        // Despawn this goal post
        Invoke(nameof(DestroySelf), 0.1f);
    }

    private void DestroySelf()
    {
        Destroy(gameObject);
    }
}