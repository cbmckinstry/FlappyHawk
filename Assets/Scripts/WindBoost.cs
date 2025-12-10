using UnityEngine;
using System.Collections;

public class WindBoost : MonoBehaviour, ICollectible
{
    public int healthGain = 1;
    public float moveSpeed = 4.5f;
    private float leftEdge;
    public static float BOOST_DISTANCE = 1.5f;
    public static float BOOST_SPEED = 0.5f;

    private bool isGravityFlip = false;
    private float colorCycleSpeed = 1.5f;
    private SpriteRenderer spriteRenderer;
    private Color normalColor = Color.white;
    private Color greenColor = new Color(0f, 1f, 0f, 1f);
    private Color redColor = new Color(1f, 0f, 0f, 1f);
    private Color blackColor = new Color(0f, 0f, 0f, 1f);

    private void OnEnable()
    {
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
            Debug.LogError("No Main Camera found in scene!");
            return;
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        leftEdge = Camera.main.ScreenToWorldPoint(Vector3.zero).x - 1f;

        if (isGravityFlip)
        {
            transform.rotation = Quaternion.AngleAxis(-90f, Vector3.forward);
            StartCoroutine(CycleColorRedBlack());
        }
        else
        {
            StartCoroutine(CycleColorWhiteGreen());
        }
    }

    private void Update()
    {
        transform.position += Vector3.left * moveSpeed * Time.deltaTime;

        if (transform.position.x < leftEdge)
            Destroy(gameObject);
    }

    public void Collect(Player player)
    {
        if (isGravityFlip)
        {
            player.ApplyGravityFlip(2f);
            AudioManager.Instance?.PlaySpeedBoost();
        }
        else
        {
            bool isAtMaxHealth = player.GetHealth() >= player.GetMaxHealth();
            player.GainHealth(healthGain);
            
            if (!isAtMaxHealth)
            {
                player.ApplyHorizontalBoost(BOOST_DISTANCE, BOOST_SPEED);
                AudioManager.Instance?.PlaySpeedBoost();
            }
        }
    }

    public void SetAsGravityFlip()
    {
        isGravityFlip = true;
    }

    public bool IsGravityFlip()
    {
        return isGravityFlip;
    }

    private IEnumerator CycleColorWhiteGreen()
    {
        while (gameObject != null)
        {
            float elapsed = 0f;
            while (elapsed < 1f / colorCycleSpeed && gameObject != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (1f / colorCycleSpeed);
                spriteRenderer.color = Color.Lerp(normalColor, greenColor, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < 1f / colorCycleSpeed && gameObject != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (1f / colorCycleSpeed);
                spriteRenderer.color = Color.Lerp(greenColor, normalColor, t);
                yield return null;
            }
        }
    }

    private IEnumerator CycleColorRedBlack()
    {
        while (gameObject != null)
        {
            float elapsed = 0f;
            while (elapsed < 1f / colorCycleSpeed && gameObject != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (1f / colorCycleSpeed);
                spriteRenderer.color = Color.Lerp(redColor, blackColor, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < 1f / colorCycleSpeed && gameObject != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (1f / colorCycleSpeed);
                spriteRenderer.color = Color.Lerp(blackColor, redColor, t);
                yield return null;
            }
        }
    }
}
