using UnityEngine;

/// <summary>
/// Simple 2D character controller using W, A, S, D keys.
/// Attach to a GameObject with a Rigidbody2D and Collider2D.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerControlerWASD : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Jumping")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private float horizontalInput;
    private bool jumpRequested;
    private bool isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // Horizontal movement: A / D
        horizontalInput = 0f;
        if (Input.GetKey(KeyCode.A)) horizontalInput -= 1f;
        if (Input.GetKey(KeyCode.D)) horizontalInput += 1f;

        // Jump: W (pressed this frame, applied in FixedUpdate)
        if (Input.GetKeyDown(KeyCode.W) && isGrounded)
        {
            jumpRequested = true;
        }

        // Flip sprite to face movement direction
        if (horizontalInput != 0f)
        {
            transform.localScale = new Vector3(
                Mathf.Sign(horizontalInput) * Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z
            );
        }

        // Note: S can be used for crouch/drop-through-platform logic if needed.
        // if (Input.GetKey(KeyCode.S)) { ... }
    }

    private void FixedUpdate()
    {
        // Ground check
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }

        // Apply horizontal velocity, keep existing vertical velocity
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);

        // Apply jump
        if (jumpRequested)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpRequested = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}