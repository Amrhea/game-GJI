using UnityEngine;

/// <summary>
/// Top-down movement for the Serial Killer. Sprint multiplies move speed while
/// stamina allows it. Uses a kinematic Rigidbody2D so walls/colliders can be added later.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Stamina))]
public class KillerMovement : MonoBehaviour
{
    [SerializeField] private KillerInput input;
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeedMultiplier = 1.6f;

    /// <summary>True while the killer actually sprinted during the last fixed update.</summary>
    public bool IsSprinting { get; private set; }

    private Rigidbody2D _body;
    private Stamina _stamina;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _stamina = GetComponent<Stamina>();
    }

    private void FixedUpdate()
    {
        Vector2 direction = input.MoveInput;
        bool isMoving = direction.sqrMagnitude > 0.0001f;
        if (isMoving)
        {
            direction.Normalize();
        }

        bool sprintRequested = input.SprintHeld && isMoving;
        bool canSprint = sprintRequested && _stamina.CanSprint();

        // Stamina is advanced here so drain/recovery run at the simulation rate.
        _stamina.Tick(sprintRequested, isMoving, Time.fixedDeltaTime);

        float speed = walkSpeed * (canSprint ? sprintSpeedMultiplier : 1f);
        _body.MovePosition(_body.position + direction * (speed * Time.fixedDeltaTime));

        IsSprinting = canSprint;
    }
}
