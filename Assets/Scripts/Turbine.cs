using UnityEngine;

/// <summary>
/// Turbine obstacle - rotates continuously while moving left.
/// Uses the same global pipe speed as other obstacles via GameManager.
/// The "Blade" child object rotates while the parent moves left.
/// </summary>
public class Turbine : MonoBehaviour
{
    public float pipeSpeed = 4.5f;
    [SerializeField] private float rotationSpeed = 360f; // degrees per second
    private float currentRotation = 0f;
    private float leftEdge;
    private Transform bladeTransform; // Reference to the "Blade" child

    private void OnEnable()
    {
        // Use current global pipe speed
        pipeSpeed = GameManager.CurrentPipeSpeed;

        // Subscribe for future difficulty/speed changes
        GameManager.OnPipeSpeedChanged += HandlePipeSpeedChanged;
    }

    private void OnDisable()
    {
        GameManager.OnPipeSpeedChanged -= HandlePipeSpeedChanged;
    }

    private void HandlePipeSpeedChanged(float newSpeed)
    {
        pipeSpeed = newSpeed;
    }

    private void Start()
    {
        // Ensure proper collision tag
        gameObject.tag = "Obstacle";

        if (Camera.main == null)
        {
            Debug.LogError("No Main Camera found in scene!");
            return;
        }

        leftEdge = Camera.main.ScreenToWorldPoint(Vector3.zero).x - 1f;

        // Find the "Blade" child for rotation animation
        bladeTransform = transform.Find("Blade");
        if (bladeTransform == null)
        {
            Debug.LogWarning("Turbine prefab missing 'Blade' child object! Rotation will not work.");
        }
    }

    private void Update()
    {
        // Move left with the game
        transform.position += Vector3.left * pipeSpeed * Time.deltaTime;

        // Rotate only the blade
        if (bladeTransform != null)
        {
            currentRotation += rotationSpeed * Time.deltaTime;
            if (currentRotation >= 360f)
                currentRotation -= 360f;

            bladeTransform.rotation = Quaternion.AngleAxis(currentRotation, Vector3.forward);
        }

        // Destroy when off-screen
        if (transform.position.x < leftEdge)
            Destroy(gameObject);
    }
}
