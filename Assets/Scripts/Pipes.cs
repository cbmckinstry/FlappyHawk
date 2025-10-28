using UnityEngine;

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
        leftEdge = Camera.main.ScreenToWorldPoint(Vector3.zero).x - 1f;
    }

    private void Update()
    {
        transform.position += Vector3.left * pipeSpeed * Time.deltaTime;
        if (transform.position.x < leftEdge)
            Destroy(gameObject);
    }
}
