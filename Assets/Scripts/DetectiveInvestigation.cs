using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Evidence investigation pass: when DetectiveDetection reports evidence, the
/// detective stops patrolling, walks to the evidence, stands there for a short
/// timer, then resumes patrol. Movement reuses DetectivePatrol's kinematic
/// Rigidbody2D step (no NavMesh, no pathfinding). Local gameplay logic only.
/// Flow: PATROL -> MOVE TO EVIDENCE -> INVESTIGATE (timer) -> RESUME PATROL.
/// If the evidence disappears at any point, investigation aborts safely.
/// </summary>
[RequireComponent(typeof(DetectivePatrol))]
[RequireComponent(typeof(DetectiveDetection))]
public class DetectiveInvestigation : MonoBehaviour
{
    private enum InvestigationState
    {
        Patrolling,
        MovingToEvidence,
        Investigating,
    }

    [Header("Investigation rules (baseline)")]
    [SerializeField] private float investigationDuration = 3f;
    [SerializeField] private float investigationArrivalThreshold = 0.75f;

    private DetectivePatrol _patrol;
    private DetectiveDetection _detection;
    private DetectiveKillerDetection _killerDetection;
    private Rigidbody2D _body;
    private InvestigationState _state = InvestigationState.Patrolling;
    private Evidence _target;
    private float _timer;
    private readonly HashSet<Evidence> _investigated = new HashSet<Evidence>();

    /// <summary>True while the detective is traveling to or inspecting evidence.</summary>
    public bool IsInvestigating => _state != InvestigationState.Patrolling;

    /// <summary>The evidence currently being investigated, or null.</summary>
    public Evidence TargetEvidence => _target;

    /// <summary>True if this specific evidence object has already been examined once.</summary>
    public bool HasAlreadyInvestigated(Evidence evidence) =>
        evidence != null && _investigated.Contains(evidence);

    /// <summary>Seconds left while inspecting; 0 otherwise.</summary>
    public float RemainingTime =>
        _state == InvestigationState.Investigating ? _timer : 0f;

    /// <summary>Current distance to the investigation target (0 when idle).</summary>
    public float DistanceToTarget
    {
        get
        {
            if (_state == InvestigationState.Patrolling || _target == null)
            {
                return 0f;
            }

            return Vector2.Distance(_body.position, _target.transform.position);
        }
    }

    private void Awake()
    {
        _patrol = GetComponent<DetectivePatrol>();
        _detection = GetComponent<DetectiveDetection>();
        _killerDetection = GetComponent<DetectiveKillerDetection>();
        _body = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        _detection.EvidenceSpotted += BeginInvestigation;
        if (_killerDetection != null)
        {
            _killerDetection.KillerDetected += AbortForKillerDetection;
        }
    }

    private void OnDisable()
    {
        _detection.EvidenceSpotted -= BeginInvestigation;
        if (_killerDetection != null)
        {
            _killerDetection.KillerDetected -= AbortForKillerDetection;
        }
    }

    private void FixedUpdate()
    {
        switch (_state)
        {
            case InvestigationState.MovingToEvidence:
                TickMoveToEvidence();
                break;
            case InvestigationState.Investigating:
                TickInvestigate();
                break;
        }
    }

    private void BeginInvestigation(Evidence evidence)
    {
        _target = evidence;
        _state = InvestigationState.MovingToEvidence;
        _timer = 0f;
        _patrol.SetPatrolPaused(true);
        Debug.Log($"[DetectiveInvestigation] Moving to {evidence.Type} at " +
                  $"({evidence.transform.position.x}, {evidence.transform.position.y})");
    }

    private void TickMoveToEvidence()
    {
        // Evidence may be destroyed mid-walk (e.g. cleaned by the Janitor).
        // Unity's overloaded == makes this true for destroyed objects too.
        if (_target == null)
        {
            EndInvestigation("Evidence gone before arrival");
            return;
        }

        float remaining = _patrol.MoveStepToward(_target.transform.position);
        if (remaining <= investigationArrivalThreshold)
        {
            _state = InvestigationState.Investigating;
            _timer = investigationDuration;
            Debug.Log($"[DetectiveInvestigation] Investigating {_target.Type} for {investigationDuration} s");
        }
    }

    private void TickInvestigate()
    {
        if (_target == null)
        {
            EndInvestigation("Evidence gone during investigation");
            return;
        }

        _timer -= Time.fixedDeltaTime;
        if (_timer > 0f)
        {
            return;
        }

        // Mark this evidence object as examined so detection stops reacting to
        // it and patrol resumes; a NEW evidence object (new kill) is fresh.
        _investigated.Add(_target);
        EndInvestigation("Investigation complete");
    }

    private void EndInvestigation(string reason)
    {
        Debug.Log($"[DetectiveInvestigation] {reason} — patrol resumed");
        _target = null;
        _state = InvestigationState.Patrolling;
        _patrol.SetPatrolPaused(false);
    }

    /// <summary>
    /// Higher priority: direct killer detection preempts evidence investigation.
    /// Aborts cleanly WITHOUT resuming patrol — DetectiveKillerDetection owns the
    /// pause while the killer is in view. Public because it is also invoked
    /// directly by DetectiveKillerDetection (in addition to the event hookup).
    /// </summary>
    public void AbortForKillerDetection()
    {
        if (_state == InvestigationState.Patrolling)
        {
            return;
        }

        _target = null;
        _state = InvestigationState.Patrolling;
        _timer = 0f;
        Debug.Log("[DetectiveInvestigation] Investigation aborted — killer detected");
    }
}