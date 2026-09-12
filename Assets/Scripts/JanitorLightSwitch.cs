using UnityEngine;

/// <summary>
/// The Janitor's instant lamp interaction (E). Lowest E priority: it only acts
/// when nothing else can use the press (not cleaning, no Blood in range, not
/// dragging, no Corpse in range). Turns the nearest lamp within interaction
/// range OFF (baseline 1.75 m); the lamp restores itself after its own timer.
/// Instant action — no movement lock, the Janitor keeps walking.
/// </summary>
[RequireComponent(typeof(JanitorMovement))]
public class JanitorLightSwitch : MonoBehaviour
{
    [SerializeField] private JanitorInput input;
    [SerializeField] private JanitorCleanup cleanup;
    [SerializeField] private JanitorCorpseDrag drag;

    /// <summary>BASELINE: max distance at which a lamp can be toggled.</summary>
    [SerializeField] private float interactionRange = 1.75f;

    [Header("Physics")]
    [SerializeField] private LayerMask lampMask = ~0;

    /// <summary>Interaction range (baseline 1.75 m).</summary>
    public float InteractionRange => interactionRange;

    private void Awake()
    {
        if (input == null)
        {
            input = GetComponent<JanitorInput>(); // self-wire when added manually
        }

        if (cleanup == null)
        {
            cleanup = GetComponent<JanitorCleanup>();
        }

        if (drag == null)
        {
            drag = GetComponent<JanitorCorpseDrag>();
        }
    }

    private void OnEnable()
    {
        if (input != null)
        {
            input.InteractPressed += OnInteractPressed;
        }
    }

    private void OnDisable()
    {
        if (input != null)
        {
            input.InteractPressed -= OnInteractPressed;
        }
    }

    /// <summary>Nearest lamp within interaction range (any state), or null.</summary>
    public JanitorLamp FindNearestLamp()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactionRange, lampMask);
        JanitorLamp nearest = null;
        float nearestSqr = float.MaxValue;
        foreach (Collider2D hit in hits)
        {
            JanitorLamp lamp = hit.GetComponent<JanitorLamp>();
            if (lamp == null)
            {
                continue;
            }

            float sqrDistance = ((Vector2)hit.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (sqrDistance < nearestSqr)
            {
                nearestSqr = sqrDistance;
                nearest = lamp;
            }
        }

        return nearest;
    }

    private void OnInteractPressed()
    {
        // Deterministic arbitration — the lamp is the lowest-priority E action.
        if (cleanup != null && (cleanup.IsCleaning || cleanup.BloodInRange))
        {
            return; // Blood cleaning outranks lamps
        }

        if (drag != null && (drag.IsDragging || drag.CanStartDrag()))
        {
            return; // Corpse dragging outranks lamps (E = cancel drag while dragging)
        }

        JanitorLamp lamp = FindNearestLamp();
        if (lamp == null)
        {
            return; // no lamp in range
        }

        if (!lamp.IsOn)
        {
            Debug.Log("[JanitorLightSwitch] Lamp is already OFF — timer keeps running (no restart)");
            return;
        }

        if (lamp.TryTurnOff())
        {
            Debug.Log($"[JanitorLightSwitch] Lamp OFF at {lamp.transform.position} — auto ON in {lamp.OffDuration:0.0} s (Janitor free to move)");
        }
        else
        {
            Debug.Log($"[JanitorLightSwitch] Lamp refused — {JanitorLamp.ActiveCount}/{JanitorLamp.TotalCount} ON, minimum {JanitorLamp.MinimumActiveCount} must stay lit");
        }
    }
}
