using UnityEngine;

public class Balloon : MonoBehaviour
{
    public float moveSpeed = 4.5f;
    [SerializeField] private float destroyOffset = 2.5f;

    private float leftEdge;

    private float bobSpeed = 1f;
    private float bobAmount = 0f;
    private float bobTimer = 0f;

    private Vector3 startPosition;
    private GameManager.Difficulty currentDifficulty;

    // NORMAL MODE SETTINGS
    private float normalRiseSpeed = 1.2f; // upward speed for Normal
    private float normalStartY;           // where Normal balloons start (for reference)

    private void OnEnable()
    {
        moveSpeed = GameManager.CurrentScrollSpeed;
        GameManager.OnScrollSpeedChanged += OnSpeedChanged;
        SetBobParameters();
    }

    private void OnDisable()
    {
        GameManager.OnScrollSpeedChanged -= OnSpeedChanged;
    }

    private void OnSpeedChanged(float s) => moveSpeed = s;

    private void SetBobParameters()
    {
        currentDifficulty = GameManager.CurrentDifficulty;

        switch (currentDifficulty)
        {
            case GameManager.Difficulty.Easy:
                // EASY: gentle bob
                bobAmount = 0.05f;
                bobSpeed  = 1.5f;
                break;

            case GameManager.Difficulty.Normal:
                // NORMAL: no bob, just rising
                bobAmount = 0f;
                bobSpeed  = 0f;
                moveSpeed *= 1.15f;
                break;

            case GameManager.Difficulty.Hard:
                bobAmount = 0.075f;
                bobSpeed  = 3f;
                break;
        }
    }

    private void Start()
    {
        gameObject.tag = "Obstacle";

        if (Camera.main == null) return;

        leftEdge = Camera.main.ScreenToWorldPoint(Vector3.zero).x - destroyOffset;
        startPosition = transform.position;

        // ⭐ NORMAL: start slightly below mid-screen
        if (currentDifficulty == GameManager.Difficulty.Normal)
        {
            // Bottom of screen in world coords
            Vector3 bottom = Camera.main.ScreenToWorldPoint(
                new Vector3(Screen.width / 2f, 0f, 0f));

            // Start halfway between bottom and the original spawn Y
            // (this places it below mid-screen but not too low)
            float startY = Mathf.Lerp(bottom.y, startPosition.y, 0.5f);

            transform.position = new Vector3(transform.position.x, startY, transform.position.z);
            normalStartY = startY;
        }

        // ⭐ HARD: start exactly at mid-screen height
        if (currentDifficulty == GameManager.Difficulty.Hard)
        {
            Vector3 mid = Camera.main.ScreenToWorldPoint(
                new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));

            transform.position = new Vector3(transform.position.x, mid.y, transform.position.z);
            startPosition = transform.position;
        }
    }

    private void Update()
    {
        Vector3 movement = Vector3.left * moveSpeed * Time.deltaTime;

        // EASY + HARD: bobbing enabled
        if (bobAmount > 0f)
        {
            bobTimer += Time.deltaTime * bobSpeed;

            float bobOffset;

            if (currentDifficulty == GameManager.Difficulty.Hard)
{
    // HARD: bob a little farther down than up, smoothly
    // 1.4f = deeper amplitude
    // 0.4f = shift whole wave slightly downward
    bobOffset = ((Mathf.Cos(bobTimer)) * 1.4f + 0.4f) * bobAmount;
}
else
{
    // EASY: regular sine bob
    bobOffset = Mathf.Sin(bobTimer) * bobAmount;
}


            // Keep your original "per-frame" vertical scaling
            movement.y = bobOffset * Time.deltaTime * 60f;
        }
        else
        {
            // NORMAL: no bob, just slowly rising upward
            if (currentDifficulty == GameManager.Difficulty.Normal)
            {
                movement.y = normalRiseSpeed * Time.deltaTime;
            }
            else
            {
                movement.y = 0f;
            }
        }

        transform.position += movement;

        if (transform.position.x < leftEdge)
            Destroy(gameObject);
    }
}
