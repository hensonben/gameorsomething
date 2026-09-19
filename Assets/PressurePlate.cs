using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A physical (non-UI) pressure-plate button. Attach this to the CHILD object
/// (the top part that gets pressed down). The PARENT object should be the
/// stationary base beneath it and needs no script at all -- just a Collider2D
/// if you want it to look/feel solid.
///
/// Setup:
/// 1. Parent: stationary base. No Rigidbody2D needed (or set to Static).
///    Give it a Collider2D so it looks/behaves solid.
/// 2. Child: this script. Needs a Rigidbody2D (script will force it to
///    Kinematic) and a Collider2D that is NOT a trigger (objects need to
///    physically rest on it for weight detection to work).
/// 3. Adjust requiredWeight to the combined mass needed to press the button.
/// 4. Hook up onPressed / onReleased in the Inspector for per-button
///    feedback (sound, animation, etc.).
/// 5. Optional: enable staysPressedPermanently if this should act as a
///    one-way latch (stays down forever once triggered, e.g. a switch
///    that permanently opens a gate) rather than popping back up when
///    the weight is removed.
/// 6. Optional: to require multiple buttons pressed together for one
///    effect (e.g. a puzzle door), drag the OTHER buttons into this
///    button's linkedButtons list (don't add itself), then wire the
///    actual effect to onAllPressed / onAllReleased instead of
///    onPressed / onReleased. Do this on every button in the group so
///    it works no matter which one is pressed last.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PressurePlate : MonoBehaviour
{
    [Header("Weight Detection")]
    [Tooltip("Combined mass required (one object or several stacked/side by " +
             "side) to press the button down.")]
    [SerializeField] private float requiredWeight = 1f;

    [Header("Movement")]
    [Tooltip("Local offset applied when fully pressed. Usually negative Y " +
             "(pressed down). Relative to the button's starting local position.")]
    [SerializeField] private Vector3 pressedOffset = new Vector3(0f, -0.2f, 0f);

    [Tooltip("How quickly the button moves between pressed and unpressed positions.")]
    [SerializeField] private float moveSpeed = 8f;

    [Tooltip("Seconds contact must be fully lost before the button releases " +
             "and pops back up. Prevents jitter when the plate sinks faster " +
             "than an object can fall with it (momentary contact loss).")]
    [SerializeField] private float releaseCooldown = 0.15f;

    [Tooltip("If true, once this button is pressed it stays pressed " +
             "permanently, even after all weight is removed -- like a " +
             "one-way latch. onReleased will never fire once it's latched.")]
    [SerializeField] private bool staysPressedPermanently = false;

    [Header("Events")]
    public UnityEvent onPressed;
    public UnityEvent onReleased;

    [Header("Linked Buttons (optional)")]
    [Tooltip("Other pressure plates that must ALSO be pressed at the same " +
             "time as this one. Leave empty for a standalone button. Only " +
             "add each button ONCE across the group -- no need to add this " +
             "button to its own list.")]
    [SerializeField] private List<PressurePlate> linkedButtons = new List<PressurePlate>();

    [Tooltip("Fires when this button AND every button in linkedButtons are " +
             "all pressed at once. Wire your actual puzzle effect (door, " +
             "gate, etc.) here instead of onPressed if this button is part " +
             "of a group.")]
    public UnityEvent onAllPressed;

    [Tooltip("Fires when the group was fully pressed and at least one " +
             "button (this one or a linked one) is no longer pressed.")]
    public UnityEvent onAllReleased;

    private Rigidbody2D rb;
    private Vector3 unpressedLocalPos;
    private Vector3 pressedLocalPos;
    private readonly HashSet<Rigidbody2D> objectsOnPlate = new HashSet<Rigidbody2D>();
    private float timeBelowThreshold;
    private bool allLinkedPressed;

    /// <summary>True while enough weight is currently on the plate.</summary>
    public bool IsPressed { get; private set; }

    /// <summary>Combined mass currently resting on the plate.</summary>
    public float CurrentWeight { get; private set; }

    /// <summary>True while this button AND all linked buttons are pressed.</summary>
    public bool AllLinkedPressed => allLinkedPressed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // This button is moved directly by script, not by physics forces,
        // so it must be Kinematic. It can still physically support objects
        // and receive collision callbacks from dynamic (non-kinematic) ones.
        if (rb.bodyType != RigidbodyType2D.Kinematic)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        unpressedLocalPos = transform.localPosition;
        pressedLocalPos = unpressedLocalPos + pressedOffset;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.rigidbody != null)
        {
            objectsOnPlate.Add(collision.rigidbody);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.rigidbody != null)
        {
            objectsOnPlate.Remove(collision.rigidbody);
        }
    }

    private void FixedUpdate()
    {
        // Clean up anything destroyed while still in contact
        objectsOnPlate.RemoveWhere(body => body == null);

        float totalWeight = 0f;
        foreach (Rigidbody2D body in objectsOnPlate)
        {
            totalWeight += body.mass;
        }
        CurrentWeight = totalWeight;

        bool weightPresent = totalWeight >= requiredWeight;

        if (weightPresent)
        {
            // Enough weight right now: press immediately and reset the
            // release timer. No debounce needed on the way down.
            timeBelowThreshold = 0f;

            if (!IsPressed)
            {
                IsPressed = true;
                onPressed?.Invoke();
            }
        }
        else if (IsPressed && !staysPressedPermanently)
        {
            // Weight dropped below threshold (possibly just a momentary
            // contact loss). Only release after it stays below threshold
            // for the full cooldown duration.
            timeBelowThreshold += Time.fixedDeltaTime;

            if (timeBelowThreshold >= releaseCooldown)
            {
                IsPressed = false;
                timeBelowThreshold = 0f;
                onReleased?.Invoke();
            }
        }
        // If staysPressedPermanently is true and IsPressed is already true,
        // we intentionally do nothing here -- it stays latched down forever.

        // Check the linked group (this button + everything in linkedButtons).
        // If linkedButtons is empty, this just mirrors IsPressed.
        bool groupPressed = IsPressed;
        if (linkedButtons != null)
        {
            for (int i = 0; i < linkedButtons.Count; i++)
            {
                PressurePlate other = linkedButtons[i];
                if (other == null || !other.IsPressed)
                {
                    groupPressed = false;
                    break;
                }
            }
        }

        if (groupPressed != allLinkedPressed)
        {
            allLinkedPressed = groupPressed;
            if (allLinkedPressed) onAllPressed?.Invoke();
            else onAllReleased?.Invoke();
        }

        // Move toward the target local position at a fixed speed
        Vector3 targetLocalPos = IsPressed ? pressedLocalPos : unpressedLocalPos;
        Vector3 newLocalPos = Vector3.MoveTowards(
            transform.localPosition,
            targetLocalPos,
            moveSpeed * Time.fixedDeltaTime
        );

        // Kinematic rigidbodies must be moved with MovePosition (world space),
        // so convert the local target back to world space first.
        Vector3 worldTarget = transform.parent != null
            ? transform.parent.TransformPoint(newLocalPos)
            : newLocalPos;

        rb.MovePosition(worldTarget);
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize pressed vs. unpressed position in the editor
        Vector3 basePos = Application.isPlaying ? (transform.parent != null
            ? transform.parent.TransformPoint(unpressedLocalPos)
            : unpressedLocalPos) : transform.position;

        Vector3 pressedWorld = Application.isPlaying ? (transform.parent != null
            ? transform.parent.TransformPoint(pressedLocalPos)
            : pressedLocalPos) : transform.position + pressedOffset;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(basePos, GetComponent<Collider2D>().bounds.size);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(pressedWorld, GetComponent<Collider2D>().bounds.size);
    }
}