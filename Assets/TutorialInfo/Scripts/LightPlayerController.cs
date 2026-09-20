using UnityEngine;

public class LightPlayerController : MonoBehaviour
{
    public float speed = 7f;
    public float airSpeedMultiplier = 0.4f;
    public float jumpForce = 6f;
    public float fallGravityMultiplier = 2.5f;
    public float crouchSpeed = 5f;
    public float crouchHeightMultiplier = 0.4f;
    public float crouchWidthMultiplier = 1.3f;

    private Rigidbody2D rb;
    private Vector3 spawnPosition;
    private float originalWidth;
    private float originalHeight;
    private float normalGravity;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spawnPosition = transform.position;
        originalWidth = transform.localScale.x;
        originalHeight = transform.localScale.y;
        normalGravity = rb.gravityScale;
    }

    void Update()
    {
        float moveX = 0f;
        if (Input.GetKey(KeyCode.LeftArrow)) moveX = -1f;
        if (Input.GetKey(KeyCode.RightArrow)) moveX = 1f;

        float currentSpeed = isGrounded ? speed : speed * airSpeedMultiplier;
        rb.linearVelocity = new Vector2(moveX * currentSpeed, rb.linearVelocity.y);

        if (Input.GetKeyDown(KeyCode.UpArrow) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        if (Input.GetKeyUp(KeyCode.UpArrow) && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }

        rb.gravityScale = (rb.linearVelocity.y < 0) ? normalGravity * fallGravityMultiplier : normalGravity;

        bool crouching = Input.GetKey(KeyCode.DownArrow);
        float targetHeight = crouching ? originalHeight * crouchHeightMultiplier : originalHeight;
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
        transform.localScale = new Vector3(originalWidth, originalHeight, 1f);
    }
}