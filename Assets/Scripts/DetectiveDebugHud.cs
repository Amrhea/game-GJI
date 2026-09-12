using UnityEngine;

/// <summary>
/// Minimal on-screen overlay to verify the detective's patrol + evidence spotting
/// during the jam. Safe to delete before shipping.
/// </summary>
public class DetectiveDebugHud : MonoBehaviour
{
    private DetectivePatrol _patrol;
    private DetectiveDetection _detection;
    private DetectiveInvestigation _investigation;
    private DetectiveKillerDetection _killerDetection;
    private DetectiveChase _chase;

    private void Awake()
    {
        _patrol = GetComponent<DetectivePatrol>();
        _detection = GetComponent<DetectiveDetection>();
        _investigation = GetComponent<DetectiveInvestigation>();
        _killerDetection = GetComponent<DetectiveKillerDetection>();
        _chase = GetComponent<DetectiveChase>();
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10f, 300f, 340f, 210f), GUI.skin.box);

        GUILayout.Label("=== DETECTIVE DEBUG ===");

        bool chasing = _chase != null && _chase.IsChasing;
        bool killerDetected = _killerDetection != null && _killerDetection.IsKillerDetected;
        bool investigating = !killerDetected && _investigation != null && _investigation.IsInvestigating;
        string state = chasing
            ? "CHASING"
            : killerDetected
                ? "KILLER DETECTED"
                : investigating
                    ? (_investigation.RemainingTime > 0f ? "INVESTIGATING" : "MOVING TO EVIDENCE")
                    : "PATROLLING";
        GUILayout.Label($"State: {state}");

        if (chasing)
        {
            Transform killer = _killerDetection.KillerTransform;
            string killerPos = killer != null
                ? $"({killer.position.x}, {killer.position.y})"
                : "(none)";
            GUILayout.Label($"Killer distance: {_killerDetection.DistanceToKiller} m  " +
                            $"chase speed {_chase.ChaseSpeed} m/s  pos {killerPos}");
        }
        else if (killerDetected)
        {
            GUILayout.Label($"Killer distance: {_killerDetection.DistanceToKiller} m  " +
                            $"LOS: {(_killerDetection.HasClearLineOfSight ? "CLEAR" : "BLOCKED")}");
        }

        if (!_patrol.HasRoute)
        {
            GUILayout.Label("Patrol: IDLE - no patrol points assigned");
        }
        else
        {
            GUILayout.Label($"Waypoint: #{_patrol.CurrentWaypointIndex + 1}/{_patrol.WaypointCount}  " +
                            $"distance {_patrol.DistanceToWaypoint}m");
        }

        GUILayout.Label($"Speed {_patrol.PatrolSpeed} m/s  " +
                        $"pos ({_patrol.transform.position.x}, {_patrol.transform.position.y})");

        if (investigating)
        {
            Evidence target = _investigation.TargetEvidence;
            string targetType = target != null ? target.Type.ToString() : "(gone)";
            GUILayout.Label($"Evidence: {targetType}  distance {_investigation.DistanceToTarget}m");
            if (_investigation.RemainingTime > 0f)
            {
                GUILayout.Label($"Investigation timer: {_investigation.RemainingTime}s");
            }
        }
        else if (_detection != null)
        {
            GUILayout.Label($"Scanning for evidence (range {_detection.DetectionRange}m) — clear");
        }
        else
        {
            GUILayout.Label("Detection: MISSING — re-run Setup Killer Prototype Scene");
        }

        GUILayout.EndArea();
    }
}
