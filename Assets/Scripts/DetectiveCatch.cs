using System;
using UnityEngine;

/// <summary>
/// Detective Catch pass: while DetectiveChase is actively pursuing the killer,
/// the catch lands when the detective-to-killer distance drops to catchRange
/// (1.0 m baseline). Catch is ONLY possible during chase — patrol, evidence
/// detection/investigation, or merely seeing the killer without evidence
/// awareness can never trigger it, even at point-blank range.
/// On catch the detective enters a terminal KILLER_CAUGHT state: chase movement
/// stops, patrol stays paused, investigation stays aborted, and a one-shot local
/// KillerCaught event fires. The killer GameObject is NOT modified in any way —
/// this pass only records the capture. No lose/game-over logic, no networking
/// (game flow / Abim's netcode may subscribe to KillerCaught later).
/// </summary>
[RequireComponent(typeof(DetectivePatrol))]
[RequireComponent(typeof(DetectiveChase))]
public class DetectiveCatch : MonoBehaviour
{
    [Header("Catch rules (baseline)")]
    [SerializeField] private float catchRange = 1f;

    private DetectivePatrol _patrol;
    private DetectiveChase _chase;
    private DetectiveInvestigation _investigation;
    private DetectiveKillerDetection _killerDetection;
    private Rigidbody2D _body;

    /// <summary>Terminal state: true once the killer has been caught. Never resets.</summary>
    public bool IsKillerCaught { get; private set; }

    /// <summary>Distance to the killer on the last fixed update (for the debug HUD).</summary>
    public float DistanceToKiller { get; private set; }

    public float CatchRange => catchRange;

    /// <summary>
    /// Raised exactly once when the catch lands. Local gameplay event only —
    /// a later game-flow system decides what losing means. This is NOT a lose
    /// implementation and stays network-agnostic.
    /// </summary>
    public event Action KillerCaught;

    private void Awake()
    {
        _patrol = GetComponent<DetectivePatrol>();
        _chase = GetComponent<DetectiveChase>();
        _investigation = GetComponent<DetectiveInvestigation>();
        _killerDetection = GetComponent<DetectiveKillerDetection>();
        _body = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        Transform killer = _killerDetection != null ? _killerDetection.KillerTransform : null;
        if (killer != null && _body != null)
        {
            DistanceToKiller = Vector2.Distance(_body.position, killer.position);
        }

        // One-shot: once caught, the detective stays in KILLER_CAUGHT forever.
        // No recovery, no repeated event, no further checks.
        if (IsKillerCaught)
        {
            return;
        }

        // CATCH ONLY DURING CHASE — every other state (patrol, investigating,
        // killer merely visible without evidence awareness) can never catch.
        if (_chase == null || !_chase.IsChasing)
        {
            return;
        }

        // Invalid killer reference: skip this update silently (no exceptions,
        // no spam). Chase itself already holds for KillerTransform.
        if (killer == null)
        {
            return;
        }

        // Still chasing but outside the catch range.
        if (DistanceToKiller > catchRange)
        {
            return;
        }

        CatchKiller();
    }

    private void CatchKiller()
    {
        IsKillerCaught = true;

        // Movement ownership: after the catch NOTHING may move the detective.
        // 1. Chase stops moving AND stops reconciling (disabled for the rest of
        //    the session — no re-enable, no recovery). Its OnDisable also
        //    unsubscribes it from detect/lost events.
        _chase.enabled = false;

        // 2. Killer detection stops driving pause/resume: the killer leaving its
        //    range can no longer resume patrol, and no further detect/lost
        //    transitions fire. IsKillerDetected simply freezes at its last value.
        if (_killerDetection != null)
        {
            _killerDetection.enabled = false;
        }

        // 3. Investigation can no longer start or tick (its OnDisable also
        //    unsubscribes from EvidenceSpotted).
        if (_investigation != null)
        {
            _investigation.enabled = false;
        }

        // 4. Patrol remains paused (chase paused it when it began). Paused patrol
        //    never moves, and no still-enabled component calls SetPatrolPaused(false):
        //    DetectiveDetection's resume path requires IsKillerDetected == false,
        //    which can no longer happen.
        _patrol.SetPatrolPaused(true);

        KillerCaught?.Invoke();
        Debug.Log($"[{nameof(DetectiveCatch)}] KILLER CAUGHT (distance {DistanceToKiller} m <= {catchRange} m) — " +
                  "detective stopped, KillerCaught fired (once)");
    }
}