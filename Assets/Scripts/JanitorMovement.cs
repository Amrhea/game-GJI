using UnityEngine;

/// <summary>
/// Top-down movement for the Janitor. Mirrors KillerMovement's kinematic
/// Rigidbody2D pattern (WASD 8-direction, normalized diagonals, MovePosition)
/// without sprint/stamina. Cleanup and interaction come in later tasks.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class JanitorMovement : MonoBehaviour
{
    [SerializeField] private JanitorInput input;

    /// <summary>
    /// BASELINE: matches the Killer's walk speed (5 m/s) so both players feel
    /// symmetric until role-specific balancing is decided.
    /// </summary>
    [SerializeField] private float walkSpeed = 5f;

    public float WalkSpeed => walkSpeed;

    /// <summary>True while another system (e.g. JanitorCleanup) owns the Janitor's position.</summary>
    public bool IsMovementLocked { get; private set; }

    private Rigidbody2D _body;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
    }

    /// <summary>Locks/unlocks movement. Only one owner at a time (cleanup).</summary>
    public void SetMovementLocked(bool locked)
    {
        IsMovementLocked = locked;
    }

    /// <summary>Speed multiplier applied on top of walkSpeed (1 = normal, 0.5 = dragging).</summary>
    private float _speedMultiplier = 1f;

    /// <summary>Temporary speed multiplier (e.g. corpse dragging). 1 = normal speed.</summary>
    public void SetSpeedMultiplier(float multiplier)
    {
        _speedMultiplier = Mathf.Max(0f, multiplier);
    }

    private void FixedUpdate()
    {
        if (IsMovementLocked)
        {
            return; // Cleanup owns the position while locked.
        }

        Vector2 direction = input.MoveInput;
        if (direction.sqrMagnitude > 0.0001f)
        {
            direction.Normalize();
            _body.MovePosition(_body.position + direction * (walkSpeed * _speedMultiplier * Time.fixedDeltaTime));
        }
        // No input -> no MovePosition call -> Janitor stops.
    }
}
