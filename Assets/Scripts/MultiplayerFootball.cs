using UnityEngine;

public class MultiplayerFootball : MonoBehaviour
{
    private Player carrier;
    private bool isCarried = false;

    // Offset while being held
    private Vector3 carryOffset = new Vector3(0.0f, -0.25f, -1f);

    // Prevent instant pickup after dropping
    private float pickupDelay = 0f;
    private const float PICKUP_DELAY_TIME = 0.25f;
    public bool PreviewMode = false;

    // For off-screen detection
    private float leftEdge;
    private float rightEdge;
    private float topEdge;
    private float bottomEdge;

    private MultiplayerManager MP => MultiplayerManager.Instance;


    private void Start()
    {
        // Cache bounds
        if (Camera.main != null)
        {
            Vector3 bl = Camera.main.ScreenToWorldPoint(Vector3.zero);
            Vector3 tr = Camera.main.ScreenToWorldPoint(
                new Vector3(Camera.main.pixelWidth, Camera.main.pixelHeight));

            leftEdge = bl.x - 1f;
            rightEdge = tr.x + 1f;
            bottomEdge = bl.y - 1f;
            topEdge = tr.y + 1f;
        }
    }



    private void Update()
    {
        if (pickupDelay > 0f)
            pickupDelay -= Time.deltaTime;

        // If being held → follow player
        if (isCarried && carrier != null)
        {
            transform.position = carrier.transform.position + carryOffset;

            // === OFF-SCREEN WHILE CARRIED = TURNOVER ON DOWNS ===
            if (IsOffScreen())
            {
                isCarried = false;
                MP.TriggerDefenseRound();
                Destroy(gameObject);
            }

            return;
        }
    }



    // ============================================================
    //  CARRY LOGIC
    // ============================================================
    public void SetCarrier(Player p)
    {
        carrier = p;
        isCarried = true;

        // Remove physics
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) Destroy(rb);
    }



    // ============================================================
    //  DROP LOGIC
    // ============================================================
    public void Drop()
    {
        if (!isCarried) return;

        isCarried = false;
        pickupDelay = PICKUP_DELAY_TIME;

        // Add physics on drop
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 1.5f;
        rb.linearVelocity = Vector2.zero;

        // Smooth + realistic drop (same as single-player)
        float forwardForce = 2.1f;   // slower → cleaner drop
        float downwardForce = 1.2f;
        Vector2 dropDir = new Vector2(1f, -1f).normalized;

        rb.AddForce(dropDir * new Vector2(forwardForce, downwardForce).magnitude,
            ForceMode2D.Impulse);

        carrier = null;
    }



    // ============================================================
    //  SCORING + PICKUP
    // ============================================================
    private void OnTriggerEnter2D(Collider2D other)
    {
        // ------------------ SCORING ------------------
        if (other.CompareTag("Scoring"))
        {
            if (isCarried && carrier != null)
            {
                MP.OnPlayerEnteredScoring(carrier);   // +7
            }
            else
            {
                MP.OnBallDroppedScored(MP.GetBallCarrier()); // +3
            }

            Destroy(gameObject);
            return;
        }


        // ------------------ PICKUP (ONLY BY REAL CARRIER) ------------------
        if (!isCarried && pickupDelay <= 0f)
        {
            Player p = other.GetComponent<Player>();

            if (p != null && MP.IsBallCarrier(p))
                SetCarrier(p);
        }
    }



    // ============================================================
    //  HELPERS
    // ============================================================
    public bool IsCarried() => isCarried;
    public bool IsCarriedBy(Player p) => isCarried && carrier == p;

    private bool IsOffScreen()
    {
        Vector3 pos = transform.position;
        return pos.x < leftEdge || pos.x > rightEdge ||
               pos.y < bottomEdge || pos.y > topEdge;
    }
}
