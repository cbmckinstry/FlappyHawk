using UnityEngine;

/// <summary>
/// Generic obstacle base class for all obstacle types (Pipes, Balloons, Silos, Turbines)
/// </summary>
public class Pipes : MonoBehaviour
{
    public float pipeSpeed = 4.5f;
    private float leftEdge;

    private void OnEnable()
    {
        // Set speed to whatever GameManager currently uses
        var gm = FindObjectOfType<GameManager>();
        if (gm != null) pipeSpeed = gm.CurrentPipeSpeed;

        // Subscribe to future difficulty changes
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
        if (Camera.main == null)
        {
            Debug.LogError("No Main Camera found in scene!");
            return;
        }
        leftEdge = Camera.main.ScreenToWorldPoint(Vector3.zero).x - 1f;
    }

    private void Update()
    {
        transform.position += Vector3.left * pipeSpeed * Time.deltaTime;
        if (transform.position.x < leftEdge)
            Destroy(gameObject);
    }

    /// <summary>
    /// Gets the current speed of the obstacle (for subclasses)
    /// </summary>
    public float GetSpeed()
    {
        return pipeSpeed;
    }
}
