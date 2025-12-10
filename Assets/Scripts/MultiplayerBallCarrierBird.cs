using UnityEngine;

public class MultiplayerBallCarrierBird : MonoBehaviour
{
    public float scrollSpeed = 4.5f;

    [Header("Flight Pattern")]
    [SerializeField] private float bobAmplitude = 0.5f;
    [SerializeField] private float bobFrequency = 1f;

    [Header("Animation")]
    [SerializeField] private Sprite[] flapSprites;
    [SerializeField] private float flapSpeed = 0.1f;

    [Header("Ball Sprite")]
    [SerializeField] private Sprite ballSprite;

    private GameObject ballObject;

    private float leftEdge;
    private float startY;
    private float bobTimer = 0f;
    private float flapTimer = 0f;
    private int flapFrame = 0;

    private SpriteRenderer spriteRenderer;

    private bool hasBeenHit = false;
    private bool hasDespawned = false;

    private MultiplayerManager MP => MultiplayerManager.Instance;

    private void OnEnable()
    {
        scrollSpeed = GameManager.CurrentScrollSpeed;
        GameManager.OnScrollSpeedChanged += HandleSpeedChanged;
    }

    private void OnDisable()
    {
        GameManager.OnScrollSpeedChanged -= HandleSpeedChanged;
    }

    private void HandleSpeedChanged(float newSpeed)
    {
        scrollSpeed = newSpeed;
    }

    private void Start()
    {
        // use a unique tag so Player doesn't treat this like a normal obstacle
        gameObject.tag = "EnemyCarrier";

        if (Camera.main == null)
            return;

        leftEdge = Camera.main.ScreenToWorldPoint(Vector3.zero).x - 1f;
        startY = transform.position.y;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        // Force MP ball-carrier to face left
        spriteRenderer.flipX = true;


        // --- Force helmet on in MP mode ---
        var helmet = transform.Find("HelmetDisplay");
        if (helmet != null)
        {
            helmet.gameObject.SetActive(true);

            // Make sure helmet renders above the body
            var hr = helmet.GetComponent<SpriteRenderer>();
            if (hr != null)
                hr.sortingOrder = spriteRenderer.sortingOrder + 1;
        }

        // Keep whatever flip / sprite you set in the prefab.
        // Attach the ball sprite under the bird:
        AttachBallSprite(ballSprite);
    }

    private void Update()
    {
        // Move left
        transform.position += Vector3.left * scrollSpeed * Time.deltaTime;

        UpdateBobbing();
        UpdateFlapAnimation();

        // Despawn off-screen → defense fails
        if (!hasDespawned && transform.position.x < leftEdge)
        {
            hasDespawned = true;
            if (MP != null && MP.InDefenseRound)
                MP.EndDefenseRound(false);
            
            AudioManager.Instance?.PlayEnemyScore();


            Destroy(gameObject);
        }
    }

    private void UpdateBobbing()
    {
        bobTimer += Time.deltaTime;
        float bobOffset = Mathf.Sin(bobTimer * bobFrequency * Mathf.PI * 2f) * bobAmplitude;

        Vector3 pos = transform.position;
        pos.y = startY + bobOffset;
        transform.position = pos;
    }

    private void UpdateFlapAnimation()
    {
        if (flapSprites == null || flapSprites.Length == 0)
            return;

        flapTimer += Time.deltaTime;

        if (flapTimer >= flapSpeed)
        {
            flapTimer = 0f;
            flapFrame = (flapFrame + 1) % flapSprites.Length;
            spriteRenderer.sprite = flapSprites[flapFrame];
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasBeenHit || hasDespawned) return;

        Player p = other.GetComponent<Player>();
        if (p != null && MP != null)
        {
            hasBeenHit = true;

            // defense win!
            Destroy(gameObject);
            MP.EndDefenseRound(true);

        }
    }

    public void AttachBallSprite(Sprite sprite)
    {
        if (sprite == null) return;

        if (ballObject == null)
        {
            ballObject = new GameObject("Ball");
            ballObject.transform.SetParent(transform);
            ballObject.transform.localPosition = new Vector3(0f, -0.2f, -0.5f);
            ballObject.transform.localScale = Vector3.one * 0.35f;

            SpriteRenderer r = ballObject.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.sortingOrder = 1;
        }
    }
}
