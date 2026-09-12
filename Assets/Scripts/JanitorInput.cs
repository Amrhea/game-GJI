using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Local input wrapper for the Janitor.
/// Follows the same pattern as KillerInput: reuses the project-wide
/// InputSystem_Actions asset (Player action map, Move + Interact actions)
/// instead of introducing a second input architecture.
/// </summary>
public class JanitorInput : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;

    [Header("Player map action names")]
    [SerializeField] private string moveAction = "Move";
    [SerializeField] private string interactAction = "Interact";

    /// <summary>Current movement input (per-axis -1..1).</summary>
    public Vector2 MoveInput { get; private set; }

    /// <summary>Raised once per interaction-button press (Interact action, keyboard E).</summary>
    public event Action InteractPressed;

    private InputAction _move;
    private InputAction _interact;
    private bool _interactHeldLastFrame;

    private void Awake()
    {
        InputActionMap map = inputActions.FindActionMap("Player", throwIfNotFound: true);
        _move = map.FindAction(moveAction, throwIfNotFound: true);
        _interact = map.FindAction(interactAction, throwIfNotFound: true);
    }

    private void OnEnable()
    {
        _move.Enable();
        _interact.Enable();
    }

    private void OnDisable()
    {
        _move.Disable();
        _interact.Disable();
    }

    private void Update()
    {
        MoveInput = _move.ReadValue<Vector2>();

        // Edge-detect the Interact button from its raw value: a quick E tap
        // fires exactly once even though the asset's Interact action uses a
        // Hold interaction (which would otherwise swallow short taps).
        bool interactHeld = _interact.ReadValue<float>() > 0.5f;
        if (interactHeld && !_interactHeldLastFrame)
        {
            InteractPressed?.Invoke();
        }

        _interactHeldLastFrame = interactHeld;
    }
}
