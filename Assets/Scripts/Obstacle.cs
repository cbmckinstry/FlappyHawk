using UnityEngine;

/// <summary>
/// Generic obstacle base class for all obstacle types (e.g., Balloons, Silos)
/// </summary>
public class Obstacle : MonoBehaviour
{
    public float scrollSpeed = 4.5f;
    [SerializeField] private float destroyOffset = 2.5f;
    private float leftEdge;

    private void OnEnable()
    {
        scrollSpeed = GameManager.CurrentScrollSpeed;
        GameManager.OnScrollSpeedChanged += HandlescrollSpeedChanged;
    }

    private void OnDisable()
    {
        GameManager.OnScrollSpeedChanged -= HandlescrollSpeedChanged;
    }

    private void HandlescrollSpeedChanged(float newSpeed) => scrollSpeed = newSpeed;

    private void Start()
    {
        gameObject.tag = "Obstacle";
        if (Camera.main == null) return;
        leftEdge = Camera.main.ScreenToWorldPoint(Vector3.zero).x - destroyOffset;
    }

    private void Update()
    {
        transform.position += Vector3.left * scrollSpeed * Time.deltaTime;
        if (transform.position.x < leftEdge)
            Destroy(gameObject);
    }
}
