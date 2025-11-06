using UnityEngine;

/// <summary>
/// Turbine obstacle - rotates continuously while moving left
/// Uses the same movement as Pipes but adds rotation animation
/// The "Blade" child object rotates while the parent moves left
/// Rotation direction depends on difficulty:
/// - Easy: Clockwise
/// - Normal (Medium): Counter-clockwise
/// - Hard: Randomly clockwise or counter-clockwise per turbine
/// </summary>
public class Turbine : MonoBehaviour
{
    public float pipeSpeed = 4.5f;
    [SerializeField] private float rotationSpeed = 360f; // degrees per second
    private float currentRotation = 0f;
    private float leftEdge;
    private Transform bladeTransform; // Reference to the "Blade" child
    private int rotationDirection = 1; // 1 for clockwise, -1 for counter-clockwise

    private void OnEnable()
    {
        // Set speed to whatever GameManager currently uses
        var gm = FindObjectOfType<GameManager>();
        if (gm != null) pipeSpeed = gm.CurrentPipeSpeed;

        // Subscribe to future difficulty changes
        GameManager.OnPipeSpeedChanged += HandlePipeSpeedChanged;
        GameManager.OnDifficultyChanged += HandleDifficultyChanged;
    }

    private void OnDisable()
    {
        GameManager.OnPipeSpeedChanged -= HandlePipeSpeedChanged;
        GameManager.OnDifficultyChanged -= HandleDifficultyChanged;
    }

    private void HandlePipeSpeedChanged(float newSpeed)
    {
        pipeSpeed = newSpeed;
    }

    private void HandleDifficultyChanged(Difficulty newDifficulty)
    {
        SetRotationDirectionByDifficulty();
    }

    private void Start()
    {
        // Ensure the tag is set correctly for collision detection
        gameObject.tag = "Obstacle";
        if (Camera.main == null)
        {
            Debug.LogError("No Main Camera found in scene!");
            return;
        }
        leftEdge = Camera.main.ScreenToWorldPoint(Vector3.zero).x - 1f;
        
        // Find the "Blade" child object (has the SpriteRenderer that rotates)
        bladeTransform = transform.Find("Blade");
        if (bladeTransform == null)
        {
            Debug.LogWarning("Turbine prefab missing 'Blade' child object! Rotation will not work.");
        }

        // Determine rotation direction based on difficulty
        SetRotationDirectionByDifficulty();
    }

    private void SetRotationDirectionByDifficulty()
    {
        var gm = FindObjectOfType<GameManager>();
        if (gm == null)
        {
            rotationDirection = 1; // Default to clockwise
            return;
        }

        switch (gm.CurrentDifficulty)
        {
            case Difficulty.Easy:
                rotationDirection = 1; // Clockwise
                break;
            case Difficulty.Normal:
                rotationDirection = -1; // Counter-clockwise
                break;
            case Difficulty.Hard:
                rotationDirection = Random.value > 0.5f ? 1 : -1; // Randomly clockwise or counter-clockwise
                break;
            default:
                rotationDirection = 1; // Default to clockwise
                break;
        }
    }

    private void Update()
    {
        // Move left with the game (parent only moves, doesn't rotate)
        transform.position += Vector3.left * pipeSpeed * Time.deltaTime;

        // Rotate only the blade child, not the parent turbine
        if (bladeTransform != null)
        {
            currentRotation += rotationSpeed * rotationDirection * Time.deltaTime;
            if (currentRotation >= 360f)
                currentRotation -= 360f;
            else if (currentRotation < 0f)
                currentRotation += 360f;

            bladeTransform.rotation = Quaternion.AngleAxis(currentRotation, Vector3.forward);
        }

        // Destroy when off screen
        if (transform.position.x < leftEdge)
            Destroy(gameObject);
    }
}