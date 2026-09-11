using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Netcode.Components;

/// <summary>
/// B1 minimal networked player movement.
/// Owner-authoritative: local input moves only the local player; NetworkTransform
/// (AuthorityMode = Owner) replicates the transform to the other player.
/// Reuses the project-wide InputSystem_Actions asset (Player/Move) — same pattern
/// as KillerInput on Ami's branch — no second input architecture.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
public class NetworkPlayerMovement : NetworkBehaviour
{
    [Header("Input (project-wide InputSystem_Actions asset)")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private float moveSpeed = 5f;

    private InputAction _move;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            // Remote player: do not read local input on this instance.
            enabled = false;
            return;
        }

        // Clearly separated test spawn positions (host left, client right).
        transform.position = IsHost ? new Vector3(-3f, 0f, 0f) : new Vector3(3f, 0f, 0f);

        _move = inputActions
            .FindActionMap("Player", throwIfNotFound: true)
            .FindAction(moveActionName, throwIfNotFound: true);
        _move.Enable();
    }

    public override void OnNetworkDespawn()
    {
        if (_move != null)
        {
            _move.Disable();
            _move = null;
        }
    }

    private void Update()
    {
        if (!IsOwner || _move == null)
        {
            return;
        }

        Vector2 input = _move.ReadValue<Vector2>();
        transform.position += (Vector3)(input * (moveSpeed * Time.deltaTime));
    }
}