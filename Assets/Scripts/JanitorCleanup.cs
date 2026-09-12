using UnityEngine;

/// <summary>
/// Blood cleaning interaction for the Janitor.
/// Baselines: interaction range 1.75 m, cleaning duration 5 s. Finds the
/// nearest Blood (EvidenceType.Blood) inside the range via a local Physics2D
/// overlap — no global radar. Cleaning locks movement and is cancelled by any
/// movement input (progress resets to 0, blood stays). The Blood GameObject is
/// destroyed only when the timer completes.
/// </summary>
[RequireComponent(typeof(JanitorMovement))]
public class JanitorCleanup : MonoBehaviour
{
    [SerializeField] private JanitorInput input;

    /// <summary>Optional cross-guard so dragging and cleaning never share the E button.</summary>
    [SerializeField] private JanitorCorpseDrag drag;

    /// <summary>BASELINE: max distance at which a Blood can be cleaned.</summary>
    [SerializeField] private float interactionRange = 1.75f;

    /// <summary>BASELINE: seconds required to clean one Blood.</summary>
    [SerializeField] private float cleaningDuration = 5f;

    [Header("Physics")]
    [SerializeField] private LayerMask evidenceMask = ~0;

    /// <summary>True while a cleaning action is in progress.</summary>
    public bool IsCleaning { get; private set; }

    /// <summary>Elapsed seconds of the current cleaning action (0 when idle).</summary>
    public float CleaningProgress { get; private set; }

    /// <summary>Total seconds required per cleaning (baseline 5 s).</summary>
    public float CleaningDuration => cleaningDuration;

    /// <summary>Interaction range (baseline 1.75 m).</summary>
    public float InteractionRange => interactionRange;

    /// <summary>True when a Blood is within interaction range — lets lower-priority
    /// E consumers (e.g. JanitorLightSwitch) know this action owns the press.</summary>
    public bool BloodInRange => FindNearestBlood() != null;

    private Evidence _target;
    private JanitorMovement _movement;

    /// <summary>Move-held state from the previous frame while cleaning — used to
    /// cancel on a new press (rising edge), not on a key that is merely still
    /// held from walking up to the Blood.</summary>
    private bool _moveHeldPrev;

    private void Awake()
    {
        _movement = GetComponent<JanitorMovement>();
        if (input == null)
        {
            input = GetComponent<JanitorInput>(); // self-wire when added manually in the editor
        }

        if (drag == null)
        {
            drag = GetComponent<JanitorCorpseDrag>(); // optional; may be absent
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
        if (input == null)
        {
            return;
        }

        input.InteractPressed -= OnInteractPressed;
    }

    private void FixedUpdate()
    {
        if (!IsCleaning)
        {
            return;
        }

        // Cancel: a NEW movement press aborts cleaning (progress resets; blood
        // stays). A key still held from walking up to the Blood does not
        // instantly cancel — only the next press does.
        bool moveHeld = input.MoveInput.sqrMagnitude > 0.0001f;
        if (moveHeld && !_moveHeldPrev)
        {
            CancelCleaning("movement input");
            return;
        }
        _moveHeldPrev = moveHeld;

        // Target destroyed mid-cleaning (e.g. removed by another system) -> stop safely.
        if (_target == null)
        {
            CancelCleaning("target gone");
            return;
        }

        CleaningProgress += Time.fixedDeltaTime;
        if (CleaningProgress < cleaningDuration)
        {
            return;
        }

        FinishCleaning();
    }

    private void OnInteractPressed()
    {
        if (IsCleaning)
        {
            return; // one interaction at a time
        }

        if (drag != null && drag.IsDragging)
        {
            return; // dragging owns the E button right now (E = cancel drag)
        }

        if (drag != null && drag.CanStartDrag())
        {
            return; // a Corpse is in range — corpse dragging takes priority for E
        }

        Evidence blood = FindNearestBlood();
        if (blood == null)
        {
            return; // nothing in range
        }

        _target = blood;
        IsCleaning = true;
        CleaningProgress = 0f;
        _moveHeldPrev = input.MoveInput.sqrMagnitude > 0.0001f;
        _movement.SetMovementLocked(true);
        Debug.Log($"[JanitorCleanup] Cleaning Blood at {blood.transform.position} (range {interactionRange:0.00} m, {cleaningDuration:0.0} s) — move to cancel");
    }

    private Evidence FindNearestBlood()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactionRange, evidenceMask);
        Evidence nearest = null;
        float nearestSqr = float.MaxValue;
        foreach (Collider2D hit in hits)
        {
            Evidence evidence = hit.GetComponent<Evidence>();
            if (evidence == null || evidence.Type != EvidenceType.Blood)
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

    private void CancelCleaning(string reason)
    {
        IsCleaning = false;
        CleaningProgress = 0f;
        _target = null;
        _movement.SetMovementLocked(false);
        Debug.Log($"[JanitorCleanup] Cleaning cancelled ({reason}) — progress reset to 0, blood stays");
    }

    private void FinishCleaning()
    {
        if (_target != null)
        {
            Vector3 at = _target.transform.position;
            Destroy(_target.gameObject);
            Debug.Log($"[JanitorCleanup] Blood removed at {at}");
        }

        IsCleaning = false;
        CleaningProgress = 0f;
        _target = null;
        _movement.SetMovementLocked(false);
    }
}
