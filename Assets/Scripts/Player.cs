using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    // Movement
    private Vector3 direction;
    public float gravity = -9.8f;
    public float strength = 1f;

    // Flap animation
    private SpriteRenderer spriteRenderer;
    public Sprite[] sprites;
    private int spriteIndex = 0;

    private void AnimateSprite()
    {
        // Advance to next sprite (wrap around)
        spriteIndex++;
        if (spriteIndex >= sprites.Length)
        {
            spriteIndex = 0;
        }
        spriteRenderer.sprite = sprites[spriteIndex];
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        // Animate wings every 0.15s
        InvokeRepeating(nameof(AnimateSprite), 0.15f, 0.15f);
    }

    private void OnEnable()
    {
        // Reset position & velocity
        Vector3 position = transform.position;
        position.y = 0f;
        transform.position = position;
        direction = Vector3.zero;
    }

    private void Update(){
    bool flap =
        Keyboard.current.spaceKey.wasPressedThisFrame ||
        (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
        (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);

    if (flap)
        direction = Vector3.up * strength;

    direction.y += gravity * Time.deltaTime;
    transform.position += direction * Time.deltaTime;
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Collisions: pipes are "Obstacle"; scoring gate is "Scoring"
        if (other.gameObject.CompareTag("Obstacle"))
        {
            FindObjectOfType<GameManager>().GameOver();
        }
        else if (other.gameObject.CompareTag("Scoring"))
        {
            FindObjectOfType<GameManager>().IncreaseScore();
        }
    }
}