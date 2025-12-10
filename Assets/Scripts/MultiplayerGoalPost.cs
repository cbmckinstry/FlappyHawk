using UnityEngine;

public class MultiplayerGoalPost : MonoBehaviour
{
    private float moveSpeed;
    private float leftEdge;

    private bool hasScored = false;
    private bool roundEnded = false;

    private void OnEnable()
    {
        // sync to global scroll speed like SP mode
        moveSpeed = GameManager.CurrentScrollSpeed;
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
            Debug.LogError("[MP GoalPost] No Main Camera!");
            return;
        }

        leftEdge = Camera.main.ScreenToWorldPoint(Vector3.zero).x - 1f;

        // Make sure collisions use the scoring tag
        gameObject.tag = "Scoring";
    }

    private void Update()
    {
        // Move in sync with ALL environment scrolling (exact SP behavior)
        transform.position += Vector3.left * moveSpeed * Time.deltaTime;

        // Missed FG = despawn + defense starts
        if (!roundEnded && transform.position.x < leftEdge)
        {
            MultiplayerManager.Instance.TriggerDefenseRound();
            roundEnded = true;
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasScored || roundEnded) return;

        var mp = MultiplayerManager.Instance;
        if (mp == null) return;

        // --- PLAYER ENTERS ---
        Player p = other.GetComponent<Player>();
        if (p != null)
        {
            // Only ball carrier can score
            if (!mp.IsBallCarrier(p))
                return;

            // Must physically have the ball for 7 points
            MultiplayerFootball fb = FindFirstObjectByType<MultiplayerFootball>();
            if (fb != null && fb.IsCarriedBy(p))
            {
                mp.OnPlayerEnteredScoring(p);
                AudioManager.Instance?.PlayTouchdown();
            }
            else
            {
                // Entered without ball = turnover
                mp.TriggerDefenseRound();
            }

            hasScored = true;
            roundEnded = true;
            Destroy(gameObject);
            return;
        }

        // --- DROPPED BALL ENTERS (3 points) ---
        MultiplayerFootball dropped = other.GetComponent<MultiplayerFootball>();
        if (dropped != null && !dropped.IsCarried())
        {
            mp.OnBallDroppedScored(mp.GetBallCarrier());
            AudioManager.Instance?.PlayFieldGoal();

            hasScored = true;
            roundEnded = true;
            Destroy(gameObject);
        }
    }
}
