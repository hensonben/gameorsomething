using UnityEngine;

public class LightPlayerController : MonoBehaviour
{
    public float speed = 7f;
    public float jumpForce = 3f;
    public float crouchSpeed = 5f;
    public float normalHeight = 1f;
    public float crouchHeight = 0.4f;
    public float crouchWidthMultiplier = 1.3f;

    private Rigidbody2D rb;
    private Vector3 spawnPosition;
    private float originalWidth;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spawnPosition = transform.position;
        originalWidth = transform.localScale.x;
    }

    void Update()
    {
        // Horizontal movement
        float moveX = 0f;
        if (Input.GetKey(KeyCode.LeftArrow)) moveX = -1f;
        if (Input.GetKey(KeyCode.RightArrow)) moveX = 1f;
        rb.linearVelocity = new Vector2(moveX * speed, rb.linearVelocity.y);

        // Jump
        if (Input.GetKeyDown(KeyCode.UpArrow) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // Crouch: shrink height, widen width, snap back on release
        bool crouching = Input.GetKey(KeyCode.DownArrow);
        float targetHeight = crouching ? crouchHeight : normalHeight;
        float targetWidth = crouching ? originalWidth * crouchWidthMultiplier : originalWidth;

        float newHeight = Mathf.MoveTowards(transform.localScale.y, targetHeight, crouchSpeed * Time.deltaTime);
        float newWidth = Mathf.MoveTowards(transform.localScale.x, targetWidth, crouchSpeed * Time.deltaTime);

        transform.localScale = new Vector3(newWidth, newHeight, 1f);
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (c.gameObject.CompareTag("Ground") || c.gameObject.CompareTag("HeavyPlayer"))
            isGrounded = true;
    }

    void OnCollisionExit2D(Collision2D c)
    {
        if (c.gameObject.CompareTag("Ground") || c.gameObject.CompareTag("HeavyPlayer"))
            isGrounded = false;
    }

    public void Respawn()
    {
        transform.position = spawnPosition;
        rb.linearVelocity = Vector2.zero;
        transform.localScale = new Vector3(originalWidth, normalHeight, 1f);
    }
}