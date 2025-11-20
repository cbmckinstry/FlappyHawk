using UnityEngine;

public class CornMagnet : MonoBehaviour, ICollectible
{
    public float moveSpeed = 4.5f;
    private float leftEdge;
    private const float MAGNET_DURATION = 10f;

    private void OnEnable()
    {
        moveSpeed = GameManager.CurrentScrollSpeed;
        GameManager.OnScrollSpeedChanged += HandleSpeedChanged;
    }

    private void OnDisable()
    {
        GameManager.OnScrollSpeedChanged -= HandleSpeedChanged;
    }

    private void HandleSpeedChanged(float newSpeed)
    {
        moveSpeed = newSpeed;
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
        transform.position += Vector3.left * moveSpeed * Time.deltaTime;

        if (transform.position.x < leftEdge)
            Destroy(gameObject);
    }

    public void Collect(Player player)
    {
        AudioManager.Instance?.PlayCornCollect();
        player.ActivateCornMagnet(MAGNET_DURATION);
    }
}
