using UnityEngine;

/// <summary>
/// Turbine obstacle - rotates continuously while moving left.
/// Uses the same global scroll speed as other obstacles via GameManager.
/// The "Blade" child object rotates while the parent moves left.
/// </summary>
public class Turbine : MonoBehaviour
{
    public float scrollSpeed = 4.5f;
    [SerializeField] private float rotationSpeed = 360f;
    private float currentRotation = 0f;
    private float leftEdge;
    private Transform bladeTransform;
    private int rotationDirection = 1;

    private void OnEnable()
    {
        scrollSpeed = GameManager.CurrentScrollSpeed;
        GameManager.OnScrollSpeedChanged += HandlescrollSpeedChanged;
        SetRotationDirection();
    }

    private void OnDisable()
    {
        GameManager.OnScrollSpeedChanged -= HandlescrollSpeedChanged;
    }

    private void HandlescrollSpeedChanged(float newSpeed)
    {
        scrollSpeed = newSpeed;
    }

    private void SetRotationDirection()
    {
        var difficulty = GameManager.CurrentDifficulty;
        switch (difficulty)
        {
            case GameManager.Difficulty.Easy:
                rotationDirection = 1;
                break;
            case GameManager.Difficulty.Normal:
                rotationDirection = -1;
                break;
            case GameManager.Difficulty.Hard:
                rotationDirection = Random.Range(0, 2) == 0 ? -1 : 1;
                break;
        }
    }

    private void Start()
    {
        gameObject.tag = "Untagged";

        if (Camera.main == null)
        {
            Debug.LogError("No Main Camera found in scene!");
            return;
        }

        leftEdge = Camera.main.ScreenToWorldPoint(Vector3.zero).x - 1f;

        bladeTransform = transform.Find("Blade");
        if (bladeTransform == null)
        {
            Debug.LogWarning("Turbine prefab missing 'Blade' child object! Rotation will not work.");
        }
        else
        {
            bladeTransform.gameObject.tag = "Obstacle";
            if (rotationDirection == -1)
            {
                currentRotation = 180f;
                bladeTransform.rotation = Quaternion.AngleAxis(currentRotation, Vector3.forward);
            }
        }
    }

    private void Update()
    {
        transform.position += Vector3.left * scrollSpeed * Time.deltaTime;

        if (bladeTransform != null)
        {
            currentRotation += rotationSpeed * rotationDirection * Time.deltaTime;
            if (currentRotation >= 360f)
                currentRotation -= 360f;
            else if (currentRotation <= -360f)
                currentRotation += 360f;

            bladeTransform.rotation = Quaternion.AngleAxis(currentRotation, Vector3.forward);
        }

        if (transform.position.x < leftEdge)
            Destroy(gameObject);
    }
}
