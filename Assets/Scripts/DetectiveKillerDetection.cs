using System;
using UnityEngine;

/// <summary>
/// Direct killer detection + 2D line of sight. Each fixed update the detective
/// checks two conditions:
///   1. killer inside the detection radius, AND
///   2. an unobstructed Physics2D.Linecast reaches the killer.
/// Detection is binary and local; there is no suspicion meter, vision cone or
/// memory. Movement interruption and the pursuit only engage once the detective
/// has evidence awareness (DetectiveInvestigation owns that state). KillerDetected/
/// KillerLost fire once per transition, so the console is not spammed every frame.
/// </summary>
[RequireComponent(typeof(DetectivePatrol))]
[RequireComponent(typeof(Rigidbody2D))]
public class DetectiveKillerDetection : MonoBehaviour
{
    [Header("Direct killer detection (baseline)")]
    [SerializeField] private float killerDetectionRange = 5f;

    [Tooltip("Colliders that block line of sight. Defaults to everything.")]
    [SerializeField] private LayerMask occlusionMask = ~0;

    private DetectivePatrol _patrol;
    private DetectiveInvestigation _investigation;
    private Rigidbody2D _body;
    private KillerIdentity _killer;
    private readonly RaycastHit2D[] _losHits = new RaycastHit2D[16];

    /// <summary>True while the killer is in range with a clear line of sight.</summary>
    public bool IsKillerDetected { get; private set; }

    /// <summary>LOS evaluated on the last fixed update (for the debug HUD).</summary>
    public bool HasClearLineOfSight { get; private set; }

    /// <summary>Distance to the killer on the last fixed update.</summary>
    public float DistanceToKiller { get; private set; }

    /// <summary>The killer's transform (current position), or null if none found.</summary>
    public Transform KillerTransform => _killer != null ? _killer.transform : null;

    public float DetectionRange => killerDetectionRange;

    /// <summary>Raised once when the killer enters the detection conditions.</summary>
    public event Action KillerDetected;

    /// <summary>Raised once when the killer leaves the detection conditions.</summary>
    public event Action KillerLost;

    private void Awake()
    {
        _patrol = GetComponent<DetectivePatrol>();
        _investigation = GetComponent<DetectiveInvestigation>();
        _body = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        _killer = FindAnyObjectByType<KillerIdentity>();
        if (_killer == null)
        {
            Debug.LogWarning($"[{nameof(DetectiveKillerDetection)}] No KillerIdentity in the scene — " +
                             "killer detection disabled. Re-run Setup Killer Prototype Scene.");
        }
    }

    private void FixedUpdate()
    {
        if (_killer == null)
        {
            return; // no killer to detect; nothing to pause or resume
        }

        Vector2 detectivePosition = _body.position;
        Vector2 killerPosition = _killer.transform.position;
        DistanceToKiller = Vector2.Distance(detectivePosition, killerPosition);

        bool inRange = DistanceToKiller <= killerDetectionRange;
        HasClearLineOfSight = inRange && LineOfSightClear(killerPosition);

        if (inRange && HasClearLineOfSight)
        {
            if (!IsKillerDetected)
            {
                // Enter detection once. Movement/pursuit only engages when the
                // detective has evidence awareness — chase is gated by it.
                IsKillerDetected = true;
                if (_investigation == null || _investigation.HasEvidenceAwareness)
                {
                    _patrol.SetPatrolPaused(true);
                    _investigation?.AbortForKillerDetection();
                    KillerDetected?.Invoke();
                    Debug.Log($"[{nameof(DetectiveKillerDetection)}] KILLER DETECTED " +
                              $"(distance {DistanceToKiller} m, LOS clear) — movement stopped");
                }
                else
                {
                    Debug.Log($"[{nameof(DetectiveKillerDetection)}] KILLER DETECTED " +
                              $"(distance {DistanceToKiller} m, LOS clear) — no evidence awareness yet, patrol continues");
                }
            }
        }
        else if (IsKillerDetected)
        {
            IsKillerDetected = false;
            if (_investigation == null || _investigation.HasEvidenceAwareness)
            {
                // Chase was actually active; release the pause so patrol resumes.
                _patrol.SetPatrolPaused(false);
                KillerLost?.Invoke();
                Debug.Log($"[{nameof(DetectiveKillerDetection)}] Killer out of view — patrol resumed");
            }
            else
            {
                // Never paused patrol (no awareness) — must not touch its pause.
                Debug.Log($"[{nameof(DetectiveKillerDetection)}] Killer out of view (no chase was active)");
            }
        }
    }

    private bool LineOfSightClear(Vector2 targetPosition)
    {
        Vector2 origin = _body.position;
        float distance = Vector2.Distance(origin, targetPosition);
        if (distance <= Mathf.Epsilon)
        {
            return true; // standing on the killer — trivially visible
        }

        var filter = new ContactFilter2D();
        filter.useTriggers = true;
        filter.useLayerMask = true;
        filter.SetLayerMask(occlusionMask);

        int count = Physics2D.Linecast(origin, targetPosition, filter, _losHits);

        Collider2D nearest = null;
        float nearestDistance = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = _losHits[i].collider;
            if (hit == null || hit.transform.root == transform.root)
            {
                continue; // skip empty slots and the detective's own colliders
            }

            if (_losHits[i].distance < nearestDistance)
            {
                nearestDistance = _losHits[i].distance;
                nearest = hit;
            }
        }

        if (nearest == null)
        {
            return true; // nothing between detective and killer
        }

        // Clear LOS only when the nearest blocking collider IS the killer.
        return nearest.transform.root == _killer.transform.root;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, killerDetectionRange);

        if (Application.isPlaying && _killer != null)
        {
            Gizmos.color = HasClearLineOfSight ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, _killer.transform.position);
        }
    }
}