using UnityEngine;

public class Silo : MonoBehaviour
{
    public float moveSpeed = 4.5f;
    [SerializeField] private float destroyOffset = 2.5f;
    private float leftEdge;

    private void OnEnable()
    {
        moveSpeed = GameManager.CurrentPipeSpeed;
        GameManager.OnPipeSpeedChanged += OnSpeedChanged;
    }

    private void OnDisable()
    {
        GameManager.OnPipeSpeedChanged -= OnSpeedChanged;
    }

    private void OnSpeedChanged(float s) => moveSpeed = s;

    private void Start()
    {
        gameObject.tag = "Obstacle";
        if (Camera.main == null) return;
        leftEdge = Camera.main.ScreenToWorldPoint(Vector3.zero).x - destroyOffset;
    }

    private void Update()
    {
        transform.position += Vector3.left * moveSpeed * Time.deltaTime;
        if (transform.position.x < leftEdge)
            Destroy(gameObject);
    }
}
