using UnityEngine;

public class HeavyPlayerController : MonoBehaviour
{
    public float speed = 3f;
    public float growSpeed = 15f;
    public float climbSpeed = 3f;
    public Vector3 normalScale = new Vector3(1f, 1f, 1f);
    public Vector3 maxScale = new Vector3(2f, 2f, 2f);

    private Rigidbody2D rb;
    private Vector3 spawnPosition;
    private float originalGravity;
    private bool isSticky;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spawnPosition = transform.position;
        transform.localScale = normalScale;
        originalGravity = rb.gravityScale;
    }

    void Update()
    {
        float moveX = 0f;
        if (Input.GetKey(KeyCode.A)) moveX = -1f;
        if (Input.GetKey(KeyCode.D)) moveX = 1f;

        if (isSticky)
        {
            // Climbing mode: no gravity, W/S move up and down the wall
            rb.gravityScale = 0f;
            float moveY = 0f;
            if (Input.GetKey(KeyCode.W)) moveY = 1f;
            if (Input.GetKey(KeyCode.S)) moveY = -1f;
            rb.linearVelocity = new Vector2(moveX * speed, moveY * climbSpeed);
        }
        else
        {
            // Normal mode: gravity restored, W/S handle grow/shrink instead
            rb.gravityScale = originalGravity;
            rb.linearVelocity = new Vector2(moveX * speed, rb.linearVelocity.y);

            Vector3 targetScale = Input.GetKey(KeyCode.W) ? maxScale : normalScale;
            transform.localScale = Vector3.MoveTowards(transform.localScale, targetScale, growSpeed * Time.deltaTime);
        }
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (c.gameObject.CompareTag("Sticky")) isSticky = true;
    }

    void OnCollisionExit2D(Collision2D c)
    {
        if (c.gameObject.CompareTag("Sticky")) isSticky = false;
    }

    public void Respawn()
    {
        transform.position = spawnPosition;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = originalGravity;
        transform.localScale = normalScale;
        isSticky = false;
    }
}