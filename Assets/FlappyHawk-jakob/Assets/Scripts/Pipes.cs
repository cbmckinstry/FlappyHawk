using UnityEngine;

/// <summary>
/// Generic obstacle base class for all obstacle types (e.g., Pipes, Balloons, Silos)
/// </summary>
public class Pipes : MonoBehaviour
{
    public float pipeSpeed = 4.5f;
    [SerializeField] private float destroyOffset = 2.5f;
    private float leftEdge;

    private void OnEnable()
    {
        pipeSpeed = GameManager.CurrentPipeSpeed;
        GameManager.OnPipeSpeedChanged += HandlePipeSpeedChanged;
    }

    private void OnDisable()
    {
        GameManager.OnPipeSpeedChanged -= HandlePipeSpeedChanged;
    }

    private void HandlePipeSpeedChanged(float newSpeed) => pipeSpeed = newSpeed;

    private void Start()
    {
        gameObject.tag = "Obstacle";
        if (Camera.main == null) return;
        leftEdge = Camera.main.ScreenToWorldPoint(Vector3.zero).x - destroyOffset;
    }

    private void Update()
    {
        transform.position += Vector3.left * pipeSpeed * Time.deltaTime;
        if (transform.position.x < leftEdge)
            Destroy(gameObject);
    }
}
