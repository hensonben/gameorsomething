using UnityEngine;

public class movementtest : MonoBehaviour
{
    public float speed = 5f;
    public float jumpForce = 7f;
    public LayerMask groundLayer;
    private Rigidbody2D rb;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Horizontal movement only — vertical is left to gravity/jumping
        float moveX = Input.GetAxis("Horizontal");
        rb.linearVelocity = new Vector2(moveX * speed, rb.linearVelocity.y);

        // Jump only when grounded
        if ((Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Space)) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);   
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            isGrounded = true;
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            isGrounded = false;
    }
}