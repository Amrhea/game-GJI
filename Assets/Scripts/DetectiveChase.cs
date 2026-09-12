using UnityEngine;

/// <summary>
/// Chase pass: when DetectiveKillerDetection raises KillerDetected, the detective
/// stops patrol/investigation movement and actively pursues the killer's CURRENT
/// position every fixed update at chaseSpeed. Movement reuses DetectivePatrol's
/// kinematic step helper (no new movement framework). Detection responsibility
/// stays in DetectiveKillerDetection; this component only reacts to its events.
/// The killer-lost transition is intentionally NOT designed here (next task) —
/// on KillerLost chase simply stops and DetectiveKillerDetection resumes patrol.
/// Local gameplay only; no networking.
/// </summary>
[RequireComponent(typeof(DetectivePatrol))]
[RequireComponent(typeof(DetectiveKillerDetection))]
public class DetectiveChase : MonoBehaviour
{
    [Header("Chase rules (baseline)")]
    [SerializeField] private float chaseSpeed = 4f;

    private DetectivePatrol _patrol;
    private DetectiveKillerDetection _killerDetection;

    /// <summary>True while actively pursuing the killer.</summary>
    public bool IsChasing { get; private set; }

    public float ChaseSpeed => chaseSpeed;

    private void Awake()
    {
        _patrol = GetComponent<DetectivePatrol>();
        _killerDetection = GetComponent<DetectiveKillerDetection>();
    }

    private void OnEnable()
    {
        if (_killerDetection == null)
        {
            Debug.LogWarning($"[{nameof(DetectiveChase)}] DetectiveKillerDetection missing — chase disabled.");
            enabled = false;
            return;
        }

        _killerDetection.KillerDetected += BeginChase;
        _killerDetection.KillerLost += EndChase;
    }

    private void OnDisable()
    {
        if (_killerDetection == null)
        {
            return;
        }

        _killerDetection.KillerDetected -= BeginChase;
        _killerDetection.KillerLost -= EndChase;
    }

    private void FixedUpdate()
    {
        if (!IsChasing)
        {
            return;
        }

        // Follow the killer's CURRENT position, re-read every fixed update.
        Transform killer = _killerDetection.KillerTransform;
        if (killer == null)
        {
            // Killer destroyed/removed mid-chase: stop safely. No recovery system here.
            EndChase();
            return;
        }

        _patrol.MoveStepToward(killer.position, chaseSpeed);
    }

    private void BeginChase()
    {
        if (IsChasing)
        {
            return;
        }

        IsChasing = true;
        // Chase owns movement now; patrol must stay paused.
        _patrol.SetPatrolPaused(true);
        Debug.Log($"[{nameof(DetectiveChase)}] CHASING at {chaseSpeed} m/s");
    }

    private void EndChase()
    {
        if (!IsChasing)
        {
            return;
        }

        IsChasing = false;
        // NOTE: patrol resume is intentionally left to DetectiveKillerDetection
        // (it unpauses on KillerLost). No search/last-known-position system here.
        Debug.Log($"[{nameof(DetectiveChase)}] Chase ended");
    }
}