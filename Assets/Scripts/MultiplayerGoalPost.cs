using UnityEngine;

public class MultiplayerGoalPost : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4.5f;
    private float leftEdge;

    private bool hasScored = false;
    private bool roundEnded = false;

    private void Start()
    {
        if (Camera.main == null)
        {
            Debug.LogError("[MP GoalPost] No Main Camera!");
            return;
        }

        leftEdge = Camera.main.ScreenToWorldPoint(Vector3.zero).x - 1f;

        // Tag as scoring trigger
        gameObject.tag = "Scoring";
    }

    private void Update()
    {
        transform.position += Vector3.left * moveSpeed * Time.deltaTime;

        // If the goalpost leaves the screen and nothing scored → missed FG
        if (!roundEnded && transform.position.x < leftEdge)
        {
            Debug.Log("[MP GoalPost] Goal missed → switching to defense.");
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

        // --- CASE 1: Player enters FG ---
        Player p = other.GetComponent<Player>();

        if (p != null)
        {
            // NON–ball-carrier entering → NO SCORE
            if (!mp.IsBallCarrier(p))
                return;

            // Ball carrier enters but MUST have the football to score
            MultiplayerFootball fb = FindFirstObjectByType<MultiplayerFootball>();

            if (fb != null && fb.IsCarriedBy(p))
            {
                // Proper touchdown (7)
                mp.OnPlayerEnteredScoring(p);
                ScoreAndEndRound();
                return;
            }
            else
            {
                // Ball carrier entered WITHOUT the ball → turnover
                Debug.Log("[MP GoalPost] Ball carrier entered goal without ball → turnover.");
                mp.TriggerDefenseRound();
                ScoreAndEndRound(false);
                return;
            }
        }

        // --- CASE 2: DROPPED FOOTBALL ENTERS FG ---
        MultiplayerFootball dropped = other.GetComponent<MultiplayerFootball>();

        if (dropped != null && !dropped.IsCarried())
        {
            Player lastCarrier = mp.GetBallCarrier();
            mp.OnBallDroppedScored(lastCarrier); // field goal (3)
            ScoreAndEndRound();
            return;
        }
    }

    private void ScoreAndEndRound(bool scored = true)
    {
        hasScored = true;
        roundEnded = true;
        Invoke(nameof(DestroySelf), 0.05f);
    }

    private void DestroySelf()
    {
        Destroy(gameObject);
    }
}
