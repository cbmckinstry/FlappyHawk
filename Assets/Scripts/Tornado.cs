using UnityEngine;

public class Tornado : MonoBehaviour
{
    [Header("Animation")]
    public Sprite[] sprites;            // Tornado animation frames
    public float frameRate = 0.1f;      // Seconds per frame

    [Header("Follow Settings")]
    public Transform target;            // The Player
    public float followSpeed = 5f;      // How quickly it catches up
    public float groundY = -0.5f;       // Ground level
    public float startOffsetX = -10.5f;   // Initial distance from the player
    public float minDistanceX = 8.25f;     // How far behind the player to stay

    private SpriteRenderer spriteRenderer;
    private int spriteIndex = 0;
    private float frameTimer = 0f;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("No SpriteRenderer found on " + gameObject.name);
            enabled = false;
            return;
        }

        // Start with first sprite
        if (sprites != null && sprites.Length > 0)
            spriteRenderer.sprite = sprites[0];
    }

    private void Start()
    {
        // Spawn on far left ground, relative to player
        if (target != null)
        {
            transform.position = new Vector3(target.position.x + startOffsetX, groundY, 0f);
        }
        else
        {
            transform.position = new Vector3(-10f, groundY, 0f);
        }
    }

    private void Update()
    {
        AnimateTornado();
        FollowPlayerWithDistance();
    }

    private void AnimateTornado()
    {
        if (sprites == null || sprites.Length == 0) return;

        frameTimer += Time.deltaTime;
        if (frameTimer >= frameRate)
        {
            frameTimer = 0f;
            spriteIndex = (spriteIndex + 1) % sprites.Length;
            spriteRenderer.sprite = sprites[spriteIndex];
        }
    }

    private void FollowPlayerWithDistance()
    {
        if (target == null) return;

        // Compute desired X position (stay behind by minDistanceX)
        float desiredX = target.position.x - minDistanceX;

        // Smoothly move toward that point
        Vector3 targetPos = new Vector3(desiredX, groundY, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);
    }
}
