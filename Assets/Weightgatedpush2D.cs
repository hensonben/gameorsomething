using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Makes a Rigidbody2D resist being pushed along a specific axis unless
/// enough combined mass is pushing against it.
///
/// Two things NOT to do here, and why:
/// - Switching to Kinematic to "lock" it: Kinematic bodies ignore gravity
///   and all forces entirely, so a "locked" object would stop falling.
/// - Just zeroing velocity along the axis each FixedUpdate: Unity's physics
///   solver resolves collisions AFTER scripts' FixedUpdate runs, using the
///   incoming object's momentum. Even starting from zero velocity, the
///   solver still applies a push impulse that frame -- zeroing next frame
///   is one step too late, so the object creeps forward instead of
///   stopping outright.
///
/// Instead, this script temporarily increases this object's mass when
/// under the weight threshold. Collision impulses scale inversely with
/// mass (deltaV = impulse / mass), so a very heavy mass makes incoming
/// pushes negligible at the solver level, before any pushing happens --
/// rather than trying to correct it afterward. Gravity is unaffected by
/// this because gravitational acceleration doesn't depend on mass
/// (a = g regardless of m), so falling behavior stays correct throughout.
///
/// Trade-off: while mass is temporarily increased, OTHER forces applied
/// with ForceMode2D.Force (e.g. a fan) will also have less effect on it
/// (a = F / m), since a locked heavy object realistically should resist
/// most things, not just pushes. If you need a specific force to still
/// work at full strength while locked, that force's magnitude would need
/// to be scaled up to compensate -- ask if you run into that case.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class WeightGatedPush2D : MonoBehaviour
{
    [Header("Push Resistance")]
    [Tooltip("Combined mass of contacting objects required to push this " +
             "object along pushAxis. Below this, movement along that axis " +
             "is cancelled every physics step.")]
    [SerializeField] private float requiredPushWeight = 5f;

    [Tooltip("The axis resistance applies to, in world space. (1,0) = only " +
             "resist horizontal pushing (typical Sokoban-style crate); " +
             "(0,1) would resist vertical pushing instead. Does not need " +
             "to be normalized, it will be normalized automatically.")]
    [SerializeField] private Vector2 pushAxis = Vector2.right;

    [Tooltip("Multiplier applied to this object's mass while under the " +
             "weight threshold. Higher = more resistant to being pushed. " +
             "1000+ is typically enough to make pushing negligible without " +
             "causing physics instability. Does not affect fall speed.")]
    [SerializeField] private float lockedMassMultiplier = 1000f;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    private Rigidbody2D rb;
    private Vector2 normalizedAxis;
    private readonly HashSet<Rigidbody2D> contacts = new HashSet<Rigidbody2D>();
    private float originalMass;
    private bool isCurrentlyLocked;

    /// <summary>Combined mass currently in contact with this object.</summary>
    public float CurrentContactWeight { get; private set; }

    /// <summary>True if enough weight is currently pushing to allow movement.</summary>
    public bool IsPushable { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        normalizedAxis = pushAxis.normalized;
        originalMass = rb.mass;

        if (normalizedAxis == Vector2.zero)
        {
            Debug.LogWarning($"{name}: pushAxis was zero, defaulting to Vector2.right.");
            normalizedAxis = Vector2.right;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.rigidbody != null)
        {
            contacts.Add(collision.rigidbody);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.rigidbody != null)
        {
            contacts.Remove(collision.rigidbody);
        }
    }

    private void FixedUpdate()
    {
        contacts.RemoveWhere(body => body == null);

        float totalWeight = 0f;
        foreach (Rigidbody2D body in contacts)
        {
            totalWeight += body.mass;
        }
        CurrentContactWeight = totalWeight;

        IsPushable = totalWeight >= requiredPushWeight;

        // Swap mass BEFORE the physics solver runs this step, so the
        // collision impulse itself is computed using the correct mass --
        // rather than trying to correct velocity after the solver already
        // applied a push.
        if (!IsPushable && !isCurrentlyLocked)
        {
            rb.mass = originalMass * lockedMassMultiplier;
            isCurrentlyLocked = true;
        }
        else if (IsPushable && isCurrentlyLocked)
        {
            rb.mass = originalMass;
            isCurrentlyLocked = false;
        }

        if (!IsPushable)
        {
            // Safety net: clean up any residual velocity along the push
            // axis (e.g. leftover from the instant the object crossed
            // below threshold). The mass increase above is what actually
            // prevents new pushes; this just tidies up existing drift.
            Vector2 velocity = rb.linearVelocity;
            float velocityAlongAxis = Vector2.Dot(velocity, normalizedAxis);
            Vector2 correctedVelocity = velocity - (normalizedAxis * velocityAlongAxis);
            rb.linearVelocity = correctedVelocity;
        }
        // If IsPushable is true, we do nothing -- normal Dynamic physics
        // resolves the push exactly as it would for any other rigidbody.
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Vector2 axis = pushAxis.normalized;
        Gizmos.color = IsPushable ? Color.green : Color.red;
        Vector3 center = transform.position;
        Gizmos.DrawLine(center - (Vector3)(Vector2)axis * 0.75f, center + (Vector3)(Vector2)axis * 0.75f);
    }
}