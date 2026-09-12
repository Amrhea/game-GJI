using UnityEngine;

/// <summary>
/// Chase pass: the detective pursues the killer ONLY when both conditions hold:
///   EvidenceAwareness == true  (a blood/corpse investigation was completed)
///   AND
///   KillerDetected == true     (existing DetectiveKillerDetection LOS)
/// Until evidence is discovered, seeing the killer does NOT stop patrol or abort
/// an in-progress investigation. Movement reuses DetectivePatrol's kinematic
/// step helper at chaseSpeed (4 m/s baseline). The killer-lost transition simply
/// returns the detective to patrol — no last-known-position, no search.
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
    private DetectiveInvestigation _investigation;

    /// <summary>True while actively pursuing the killer.</summary>
    public bool IsChasing { get; private set; }

    public float ChaseSpeed => chaseSpeed;

    private void Awake()
    {
        _patrol = GetComponent<DetectivePatrol>();
        _killerDetection = GetComponent<DetectiveKillerDetection>();
        _investigation = GetComponent<DetectiveInvestigation>();
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
        // Reconcile every fixed update so chase correctly begins the moment
        // awareness becomes true while the killer is already in view, and safely
        // ends when the killer leaves the detection conditions.
        bool aware = _investigation == null || _investigation.HasEvidenceAwareness;
        bool shouldChase = aware && _killerDetection.IsKillerDetected && _killerDetection.KillerTransform != null;

        if (shouldChase && !IsChasing)
        {
            BeginChase();
        }
        else if (!shouldChase && IsChasing)
        {
            EndChase();
        }

        if (!IsChasing)
        {
            return;
        }

        // Follow the killer's CURRENT position, re-read every fixed update.
        _patrol.MoveStepToward(_killerDetection.KillerTransform.position, chaseSpeed);
    }

    private void BeginChase()
    {
        if (IsChasing)
        {
            return;
        }

        IsChasing = true;
        // Chase owns movement now: patrol stays paused and any running evidence
        // investigation is preempted so nothing fights over MovePosition.
        _patrol.SetPatrolPaused(true);
        _investigation?.AbortForKillerDetection();
        Debug.Log($"[{nameof(DetectiveChase)}] CHASING at {chaseSpeed} m/s");
    }

    private void EndChase()
    {
        if (!IsChasing)
        {
            return;
        }

        IsChasing = false;
        // Patrol resume is handled by DetectiveKillerDetection (it releases the
        // pause on KillerLost). No search/last-known-position system here.
        Debug.Log($"[{nameof(DetectiveChase)}] Chase ended");
    }
}