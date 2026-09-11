using System;
using UnityEngine;

/// <summary>
/// Detective observation pass: while patrolling, the detective continuously checks
/// a small detection circle for Evidence (Blood/Corpse). On contact it flags the
/// evidence and pauses the patrol, then reports it to DetectiveInvestigation
/// (via EvidenceSpotted), which handles the investigation and later resume.
/// No LOS, no chase, no networking.
/// </summary>
[RequireComponent(typeof(DetectivePatrol))]
[RequireComponent(typeof(Rigidbody2D))]
public class DetectiveDetection : MonoBehaviour
{
    [Header("Observation rules (baseline)")]
    [SerializeField] private float detectionRange = 1.5f;
    [SerializeField] private LayerMask evidenceMask = ~0;

    private DetectivePatrol _patrol;
    private DetectiveInvestigation _investigation;
    private DetectiveKillerDetection _killerDetection;
    private Rigidbody2D _body;
    private Evidence _spotted;

    public float DetectionRange => detectionRange;

    /// <summary>True while the detective currently has evidence in range.</summary>
    public bool HasSpottedEvidence => _spotted != null;

    /// <summary>The evidence currently observed, or null.</summary>
    public Evidence SpottedEvidence => _spotted;

    /// <summary>Raised when new evidence is noticed. Argument is the evidence.</summary>
    public event Action<Evidence> EvidenceSpotted;

    private void Awake()
    {
        _patrol = GetComponent<DetectivePatrol>();
        _body = GetComponent<Rigidbody2D>();
        _investigation = GetComponent<DetectiveInvestigation>();
        _killerDetection = GetComponent<DetectiveKillerDetection>();
        Debug.Log($"[DetectiveDetection] Active — scan range {detectionRange} m, mask {evidenceMask.value}");
    }

    private void FixedUpdate()
    {
        Evidence now = FindEvidenceInRange();

        if (now != null)
        {
            if (now != _spotted)
            {
                _spotted = now;
                _patrol.SetPatrolPaused(true);
                EvidenceSpotted?.Invoke(now);
                Debug.Log($"[DetectiveDetection] EVIDENCE SPOTTED: {now.Type} at ({now.transform.position.x}, {now.transform.position.y}) — patrol paused");
            }
        }
        else if (_spotted != null
                 && (_investigation == null || !_investigation.IsInvestigating)
                 && (_killerDetection == null || !_killerDetection.IsKillerDetected))
        {
            // Only the old detect-only behavior resumes patrol here. While an
            // investigation runs, DetectiveInvestigation owns pause/resume; while
            // the killer is directly detected, DetectiveKillerDetection owns it.
            _spotted = null;
            _patrol.SetPatrolPaused(false);
            Debug.Log("[DetectiveDetection] Evidence clear — patrol resumed");
        }
    }

    private Evidence FindEvidenceInRange()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(_body.position, detectionRange, evidenceMask);
        Evidence best = null;
        float bestSqrDistance = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            Evidence evidence = hit.GetComponentInParent<Evidence>();
            if (evidence == null || !evidence.gameObject.activeSelf || evidence.transform.root == transform.root)
            {
                continue;
            }

            if (_investigation != null && _investigation.HasAlreadyInvestigated(evidence))
            {
                continue; // Already examined — ignore it so patrol can resume.
            }

            float sqrDistance = ((Vector2)evidence.transform.position - _body.position).sqrMagnitude;
            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                best = evidence;
            }
        }

        return best;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}