using UnityEngine;

public class ObstacleMovement : MonoBehaviour
{
    public float speed = 4f;
    public float destroyX = -12f;

    private void Update()
    {
        transform.position += Vector3.left * speed * Time.deltaTime;
        if (transform.position.x < destroyX)
            Destroy(gameObject);
    }
}
