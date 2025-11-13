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
        var difficulty = GameManager.CurrentDifficulty;
        switch (difficulty)
        {
            case GameManager.Difficulty.Easy:
                bobAmount = 0f;
                bobSpeed = 0f;
                break;
            case GameManager.Difficulty.Normal:
                bobAmount = 0.05f;
                bobSpeed = 1.5f;
                break;
            case GameManager.Difficulty.Hard:
                bobAmount = .1f;
                bobSpeed = 1.5f;
                break;
        }
    }

    private void Start()
    {
        gameObject.tag = "Obstacle";
        if (Camera.main == null) return;
        leftEdge = Camera.main.ScreenToWorldPoint(Vector3.zero).x - destroyOffset;
        startPosition = transform.position;
    }

    private void Update()
    {
        Vector3 movement = Vector3.left * moveSpeed * Time.deltaTime;
        
        if (bobAmount > 0f)
        {
            bobTimer += Time.deltaTime * bobSpeed;
            float bobOffset = Mathf.Sin(bobTimer) * bobAmount;
            movement.y = bobOffset * Time.deltaTime * 60f;
        }
        
        transform.position += movement;
        
        if (transform.position.x < leftEdge)
            Destroy(gameObject);
    }
}
