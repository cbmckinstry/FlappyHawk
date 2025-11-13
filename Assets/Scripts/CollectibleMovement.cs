using UnityEngine;

public class CollectibleMovement : MonoBehaviour
{
    public float speed = 3f;
    public float destroyX = -12f;

    private void Update()
    {
        transform.position += Vector3.left * speed * Time.deltaTime;
        if (transform.position.x < destroyX)
            Destroy(gameObject);
    }
}
