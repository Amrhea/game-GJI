using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Local input wrapper for the Serial Killer.
/// Reuses the project-wide InputSystem_Actions asset (Player action map) instead of
/// introducing a second input architecture. Exposes input as plain data/events so
/// gameplay systems (and later networking) stay decoupled from input.
/// </summary>
public class KillerInput : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;

    [Header("Player map action names")]
    [SerializeField] private string moveAction = "Move";
    [SerializeField] private string sprintAction = "Sprint";
    [SerializeField] private string killAction = "Kill";

    /// <summary>Current movement input (per-axis -1..1).</summary>
    public Vector2 MoveInput { get; private set; }

    /// <summary>True while the sprint button is held.</summary>
    public bool SprintHeld { get; private set; }

    /// <summary>Raised once per kill-button press (Space).</summary>
    public event Action KillPressed;

    private InputAction _move;
    private InputAction _sprint;
    private InputAction _kill;

    private void Awake()
    {
        InputActionMap map = inputActions.FindActionMap("Player", throwIfNotFound: true);
        _move = map.FindAction(moveAction, throwIfNotFound: true);
        _sprint = map.FindAction(sprintAction, throwIfNotFound: true);
        _kill = map.FindAction(killAction, throwIfNotFound: true);
    }

    private void OnEnable()
    {
        _move.Enable();
        _sprint.Enable();
        _kill.Enable();
        _kill.performed += OnKillPerformed;
    }

    private void OnDisable()
    {
        _kill.performed -= OnKillPerformed;
        _move.Disable();
        _sprint.Disable();
        _kill.Disable();
    }

    private void Update()
    {
        MoveInput = _move.ReadValue<Vector2>();
        SprintHeld = _sprint.IsPressed();
    }

    private void OnKillPerformed(InputAction.CallbackContext context)
    {
        KillPressed?.Invoke();
    }
}
