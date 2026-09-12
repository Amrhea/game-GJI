using System;
using UnityEngine;

/// <summary>
/// Corpse dragging + disposal for the Janitor.
/// Baselines: interaction range 1.75 m (same as blood cleaning), drag speed =
/// 50% of normal walk speed (2.5 m/s). E near a Corpse starts dragging the
/// nearest one; the corpse trails behind the Janitor via simple transform
/// positioning (no joints, no parenting). E again cancels (corpse stays where
/// it was, normal speed restored). WASD keeps moving the Janitor while
/// dragging. When the dragged corpse enters the DisposalZone trigger it is
/// destroyed immediately (no button, no timer) and the local CorpseDisposed
/// event fires exactly once per corpse.
/// </summary>
[RequireComponent(typeof(JanitorMovement))]
public class JanitorCorpseDrag : MonoBehaviour
{
    [SerializeField] private JanitorInput input;

    /// <summary>BASELINE: max distance at which a Corpse can be grabbed.</summary>
    [SerializeField] private float interactionRange = 1.75f;

    /// <summary>BASELINE: fraction of normal walk speed while dragging (0.5 -> 2.5 m/s).</summary>
    [SerializeField] private float dragSpeedMultiplier = 0.5f;

    /// <summary>Distance kept between the Janitor and the dragged corpse.</summary>
    [SerializeField] private float followDistance = 1.0f;

    [Header("Physics")]
    [SerializeField] private LayerMask evidenceMask = ~0;

    /// <summary>Trigger area that instantly disposes a dragged corpse.</summary>
    [SerializeField] private Collider2D disposalZone;

    /// <summary>True while a corpse is being dragged.</summary>
    public bool IsDragging { get; private set; }

    /// <summary>Current dragged corpse (null while idle).</summary>
    public Evidence DraggedCorpse => _target;

    /// <summary>Effective movement speed while dragging (baseline 2.5 m/s).</summary>
    public float DragSpeed => _movement.WalkSpeed * dragSpeedMultiplier;

    /// <summary>Interaction range (baseline 1.75 m).</summary>
    public float InteractionRange => interactionRange;

    /// <summary>Raised once per disposed corpse (payload: the corpse GameObject).</summary>
    public event Action<GameObject> CorpseDisposed;

    private Evidence _target;
    private JanitorMovement _movement;
    private JanitorCleanup _cleanup;
    private Vector2 _followDirection = Vector2.down;

    private void Awake()
    {
        _movement = GetComponent<JanitorMovement>();
        _cleanup = GetComponent<JanitorCleanup>(); // optional cross-guard
        if (input == null)
        {
            input = GetComponent<JanitorInput>(); // self-wire when added manually
        }
    }

    private void OnEnable()
    {
        if (input == null)
        {
            return; // no JanitorInput on this object — stay idle instead of throwing
        }

        input.InteractPressed += OnInteractPressed;
    }

    private void OnDisable()
    {
        if (input != null)
        {
            input.InteractPressed -= OnInteractPressed;
        }

        if (IsDragging)
        {
            EndDrag("component disabled"); // never leave the speed multiplier applied
        }
    }

    private void FixedUpdate()
    {
        if (!IsDragging)
        {
            return;
        }

        // Corpse destroyed mid-drag -> stop safely, no exceptions.
        if (_target == null)
        {
            EndDrag("corpse gone");
            return;
        }

        // Disposal: ONLY a dragged corpse inside the zone is destroyed, instantly.
        if (disposalZone != null && disposalZone.enabled && disposalZone.OverlapPoint(_target.transform.position))
        {
            DisposeCorpse();
            return;
        }

        // Follow: trail behind the Janitor at a fixed distance (simple transform
        // positioning — no joints, no parenting, no physics simulation).
        Vector2 move = input.MoveInput;
        if (move.sqrMagnitude > 0.0001f)
        {
            _followDirection = -move.normalized; // corpse trails behind movement
        }

        _target.transform.position = (Vector2)transform.position + _followDirection * followDistance;
    }

    private void OnInteractPressed()
    {
        if (_cleanup != null && _cleanup.IsCleaning)
        {
            return; // cleaning owns the interaction right now
        }

        if (IsDragging)
        {
            EndDrag("cancelled by E");
            return;
        }

        Evidence corpse = FindNearestCorpse();
        if (corpse == null)
        {
            return; // nothing in range
        }

        _target = corpse;
        IsDragging = true;
        _followDirection = DirectionToCorpse(corpse);
        _movement.SetSpeedMultiplier(dragSpeedMultiplier);
        Debug.Log($"[JanitorCorpseDrag] Dragging Corpse at {corpse.transform.position} (drag speed {DragSpeed:0.0} m/s) — E to drop, carry it into the DisposalZone");
    }

    /// <summary>True when E could start a drag right now — used by JanitorCleanup
    /// to give an in-range corpse priority over blood cleaning (deterministic
    /// button arbitration, independent of subscription order).</summary>
    public bool CanStartDrag()
    {
        return !IsDragging
            && (_cleanup == null || !_cleanup.IsCleaning)
            && FindNearestCorpse() != null;
    }

    private void EndDrag(string reason)
    {
        IsDragging = false;
        _target = null;
        _movement.SetSpeedMultiplier(1f);
        Debug.Log($"[JanitorCorpseDrag] Drag ended ({reason}) — Janitor back to normal speed");
    }

    private void DisposeCorpse()
    {
        GameObject corpse = _target.gameObject;
        Vector3 at = corpse.transform.position;

        IsDragging = false;
        _target = null;
        _movement.SetSpeedMultiplier(1f);

        CorpseDisposed?.Invoke(corpse); // handlers may still read the object
        Destroy(corpse);
        Debug.Log($"[JanitorCorpseDrag] Corpse disposed at {at} — CorpseDisposed fired (once), Janitor back to normal speed");
    }

    private Evidence FindNearestCorpse()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactionRange, evidenceMask);
        Evidence nearest = null;
        float nearestSqr = float.MaxValue;
        foreach (Collider2D hit in hits)
        {
            Evidence evidence = hit.GetComponent<Evidence>();
            if (evidence == null || evidence.Type != EvidenceType.Corpse)
            {
                continue;
            }

            float sqrDistance = ((Vector2)hit.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (sqrDistance < nearestSqr)
            {
                nearestSqr = sqrDistance;
                nearest = evidence;
            }
        }

        return nearest;
    }

    private Vector2 DirectionToCorpse(Evidence corpse)
    {
        Vector2 toCorpse = (Vector2)corpse.transform.position - (Vector2)transform.position;
        return toCorpse.sqrMagnitude > 0.0001f ? toCorpse.normalized : Vector2.down;
    }
}
