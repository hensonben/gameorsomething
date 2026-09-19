using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A 2D fan that pushes Rigidbody2D objects away from it (by default, upward
/// in the fan's local "up" direction). Force strength falls off with distance.
///
/// Setup:
/// 1. Attach to a GameObject with a Collider2D set to "Is Trigger" = true.
///    Shape/size of that collider defines the fan's detection zone.
/// 2. The collider's bounds should roughly match "effectRange" for best results
///    (e.g. a tall BoxCollider2D or CircleCollider2D pointing in blowDirection).
/// 3. Objects need a Rigidbody2D to be affected.
/// </summary>
public class Fan2D : MonoBehaviour
{
    [Header("Force Settings")]
    [Tooltip("Maximum force applied to an object right next to the fan.")]
    [SerializeField] private float maxForce = 20f;

    [Tooltip("Distance at which force falls off to zero.")]
    [SerializeField] private float effectRange = 5f;

    [Tooltip("Direction the fan blows in, relative to the fan's own rotation. " +
             "(0, 1) = local up.")]
    [SerializeField] private Vector2 blowDirection = Vector2.up;

    [Header("Stabilization")]
    [Tooltip("Damps velocity along the blow direction so objects settle into a " +
             "hover near the top of the effect instead of bouncing up and down. " +
             "0 = no damping (will oscillate). Try 2-6 as a starting point.")]
    [SerializeField] private float verticalDamping = 3f;

    [Header("Falloff")]
    [Tooltip("How force decreases with distance. Linear = simple, Quadratic = " +
             "falls off faster near the edge of range, InverseSquare = strong " +
             "near the fan, weak farther out (like real airflow).")]
    [SerializeField] private FalloffType falloffType = FalloffType.Linear;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    public enum FalloffType
    {
        Linear,
        Quadratic,
        InverseSquare
    }

    // Objects currently inside the fan's trigger zone
    private readonly List<Rigidbody2D> objectsInRange = new List<Rigidbody2D>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        Rigidbody2D rb = other.attachedRigidbody;
        if (rb != null && !objectsInRange.Contains(rb))
        {
            objectsInRange.Add(rb);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Rigidbody2D rb = other.attachedRigidbody;
        if (rb != null)
        {
            objectsInRange.Remove(rb);
        }
    }

    private void FixedUpdate()
    {
        // Clean up any destroyed/removed rigidbodies
        objectsInRange.RemoveAll(rb => rb == null);

        Vector2 worldBlowDir = transform.TransformDirection(blowDirection).normalized;

        foreach (Rigidbody2D rb in objectsInRange)
        {
            Vector2 bottom = new Vector2(transform.position.x, transform.position.y - (transform.localScale.y / 2));
            float distance = Vector2.Distance(rb.position, bottom);

            // Beyond effect range: no force
            if (distance >= effectRange) continue;

            float strength = CalculateFalloff(distance);
            float forceMagnitude = maxForce * strength;

            // ForceMode2D.Force respects mass: F = m * a, so heavier objects
            // accelerate less than lighter ones under the same applied force.
            rb.AddForce(worldBlowDir * forceMagnitude, ForceMode2D.Force);

            // Damp velocity along the blow axis only, so the object settles
            // into a hover instead of oscillating up/down past the balance
            // point. Horizontal velocity (perpendicular to blowDirection) is
            // left untouched.
            float velocityAlongAxis = Vector2.Dot(rb.linearVelocity, worldBlowDir);
            rb.AddForce(-worldBlowDir * velocityAlongAxis * verticalDamping, ForceMode2D.Force);
        }
    }

    private float CalculateFalloff(float distance)
    {
        float t = Mathf.Clamp01(distance / effectRange); // 0 = at fan, 1 = at edge

        switch (falloffType)
        {
            case FalloffType.Quadratic:
                return 1f - (t * t);

            case FalloffType.InverseSquare:
                // Avoid divide-by-zero right at the fan's origin
                float safeDistance = Mathf.Max(distance, 0.25f);
                float normalizedInverse = 1f / (safeDistance * safeDistance);
                float maxInverse = 1f / (0.25f * 0.25f);
                return Mathf.Clamp01(normalizedInverse / maxInverse);

            case FalloffType.Linear:
            default:
                return 1f - t;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Vector2 bottom = new Vector2(transform.position.x, transform.position.y - (transform.localScale.y/2));

        Vector2 worldBlowDir = transform.TransformDirection(blowDirection).normalized;

        // Draw effect range
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f);
        Gizmos.DrawWireSphere(bottom, effectRange);

        // Draw blow direction arrow
        Gizmos.color = Color.cyan;
        Vector3 arrowEnd = transform.position + (Vector3)(worldBlowDir * effectRange);
        Gizmos.DrawLine(transform.position, arrowEnd);
        Gizmos.DrawSphere(arrowEnd, 0.15f);
    }
}