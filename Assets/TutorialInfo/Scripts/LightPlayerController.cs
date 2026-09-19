using UnityEngine;

public class LightPlayerController : MonoBehaviour
{
    public float speed = 7f;
    public float jumpForce = 10f;
    public float crouchSpeed = 5f;
    public float normalHeight = 1f;
    public float crouchHeight = 0.4f;

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

        // Crouch while Down is held, snap back the moment it's released
        float targetHeight = Input.GetKey(KeyCode.DownArrow) ? crouchHeight : normalHeight;
        float currentHeight = transform.localScale.y;
        float newHeight = Mathf.MoveTowards(currentHeight, targetHeight, crouchSpeed * Time.deltaTime);

        transform.localScale = new Vector3(originalWidth, newHeight, 1f);
    }

    void OnCollisionEnter2D(Collision2D c) { if (c.gameObject.CompareTag("Ground")) isGrounded = true; }
    void OnCollisionExit2D(Collision2D c) { if (c.gameObject.CompareTag("Ground")) isGrounded = false; }

    public void Respawn()
    {
        transform.position = spawnPosition;
        rb.linearVelocity = Vector2.zero;
        transform.localScale = new Vector3(originalWidth, normalHeight, 1f);
    }
}