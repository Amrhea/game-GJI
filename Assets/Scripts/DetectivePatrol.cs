using UnityEngine;

/// <summary>
/// Baseline Detective AI: patrols a designer-authored route in order, then wraps
/// back to the first point (A→B→C→D→A…). Straight-line movement via a kinematic
/// Rigidbody2D — the same movement pattern as KillerMovement. No NavMesh; points
/// must be placed with a clear straight path between neighbours.
/// Patrol ONLY: no detection, suspicion, line of sight or chase here.
/// Patrol can be externally paused/resumed (see DetectiveDetection).
///
/// ROUTE AUTHORING (designer-friendly — no code, no array editing):
///   Assign the route parent to `patrolRoute` (or run
///   Tools > Game Jam > Setup Killer Prototype Scene, which wires it up). The
///   parent's direct children, in Hierarchy order, define the patrol order:
///       Patrol Route
///       ├── Point 01
///       ├── Point 02
///       ├── Point 03
///       └── Point 04
///   · Add a point    → duplicate an existing child, or create an Empty
///                       GameObject under the route (optionally with a
///                       DetectivePatrolPoint marker for Scene-view clarity).
///   · Remove a point → delete the child; the route adapts automatically.
///   · Move a point   → drag it in the Scene view.
///   · Reorder        → reorder the children in the Hierarchy.
///   The route is read live from the scene, so all changes apply at the next Play.
///
/// LEGACY: when `patrolRoute` is not assigned, the previously serialized
/// `waypoints` array is used unchanged, so saved scenes keep working.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class DetectivePatrol : MonoBehaviour
{
    [Header("Route (preferred: drag the 'Patrol Route' parent here)")]
    [SerializeField] private Transform patrolRoute;

    [Header("Route (legacy serialized array — only when PatrolRoute is unset)")]
    [SerializeField] private Transform[] waypoints;

    [Header("Movement rules (baseline)")]
    [SerializeField] private float patrolSpeed = 2.5f;
    [SerializeField] private float arrivalThreshold = 0.15f;

    private Rigidbody2D _body;
    private int _waypointIndex;
    private int _lapCount;
    private float _distanceToWaypoint;
    private bool _warnedNoRoute;
    private bool _paused;

    public float PatrolSpeed => patrolSpeed;
    public float ArrivalThreshold => arrivalThreshold;

    /// <summary>Number of patrol points in the current route.</summary>
    public int WaypointCount => RoutePoints().Length;

    /// <summary>Index of the point the detective currently travels toward.</summary>
    public int CurrentWaypointIndex => _waypointIndex;

    /// <summary>True while a usable route exists (at least one non-null point).</summary>
    public bool HasRoute =>
        WaypointCount > 0 && System.Array.Exists(RoutePoints(), p => p != null);

    /// <summary>Distance to the point currently traveled toward (for the debug HUD).</summary>
    public float DistanceToWaypoint => _distanceToWaypoint;

    /// <summary>True while patrol movement is suspended (e.g. evidence spotted).</summary>
    public bool IsPatrolPaused => _paused;

    /// <summary>
    /// Moves one kinematic step toward the destination at patrol speed, with the
    /// same step clamping as the route patrol. Returns the remaining distance.
    /// Convenience overload for callers that want the baseline patrol speed.
    /// </summary>
    public float MoveStepToward(Vector2 destination) => MoveStepToward(destination, patrolSpeed);

    /// <summary>
    /// Moves one kinematic step toward the destination at the given speed, with the
    /// same step clamping as the route patrol. Returns the remaining distance after
    /// the step. Shared by evidence investigation and chase so movement logic stays
    /// in one place.
    /// </summary>
    public float MoveStepToward(Vector2 destination, float speed)
    {
        Vector2 position = _body.position;
        Vector2 toDestination = destination - position;
        float distance = toDestination.magnitude;

        if (distance <= Mathf.Epsilon)
        {
            return 0f;
        }

        float step = Mathf.Min(speed * Time.fixedDeltaTime, distance);
        _body.MovePosition(position + toDestination / distance * step);
        return distance - step;
    }

    /// <summary>Stops/starts patrol movement. The route index is kept, not reset.</summary>
    public void SetPatrolPaused(bool paused)
    {
        _paused = paused;
    }

    /// <summary>
    /// Points the detective currently patrols. Prefers the assigned Patrol Route
    /// parent — its direct children in Hierarchy order — so the route can be
    /// authored live in the scene (add / remove / reorder / move). Falls back to
    /// the legacy serialized `waypoints` array so older saved scenes keep working.
    /// </summary>
    private Transform[] RoutePoints()
    {
        if (patrolRoute != null)
        {
            int count = 0;
            foreach (Transform child in patrolRoute)
            {
                count++;
            }

            var points = new Transform[count];
            int index = 0;
            foreach (Transform child in patrolRoute)
            {
                points[index++] = child;
            }
            return points;
        }

        return waypoints != null ? waypoints : new Transform[0];
    }

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();

        if (HasRoute)
        {
            Debug.Log($"[DetectivePatrol] Route: {DescribeRoute()} — speed {patrolSpeed} m/s, arrival threshold {arrivalThreshold} m");
        }
    }

    private void FixedUpdate()
    {
        Transform[] points = RoutePoints();

        if (!HasRoute)
        {
            if (!_warnedNoRoute)
            {
                _warnedNoRoute = true;
                Debug.LogWarning($"[{nameof(DetectivePatrol)}] No patrol points assigned — detective stands still.");
            }
            return;
        }

        // The route can change between frames (points added/removed live) — keep the index in range.
        if (_waypointIndex >= points.Length)
        {
            _waypointIndex = _waypointIndex % points.Length;
        }

        if (_paused)
        {
            return;
        }

        Transform waypoint = points[_waypointIndex];
        if (waypoint == null)
        {
            // Legacy array only: point was cleared/removed — skip it instead of freezing.
            AdvanceWaypoint(points.Length);
            return;
        }

        Vector2 position = _body.position;
        Vector2 toWaypoint = (Vector2)waypoint.position - position;
        _distanceToWaypoint = toWaypoint.magnitude;

        if (_distanceToWaypoint <= arrivalThreshold)
        {
            string reached = waypoint.name;
            int reachedPosition = _waypointIndex + 1;
            bool wrapped = _waypointIndex == points.Length - 1;
            AdvanceWaypoint(points.Length);
            Debug.Log($"[DetectivePatrol] Reached {reached} (#{reachedPosition}/{points.Length}) — next: {NextWaypointName(points)}" +
                      (wrapped ? $"  | lap #{_lapCount} complete" : ""));
            return;
        }

        // Clamp the step so the detective never overshoots the point.
        float step = Mathf.Min(patrolSpeed * Time.fixedDeltaTime, _distanceToWaypoint);
        _body.MovePosition(position + toWaypoint / _distanceToWaypoint * step);
    }

    private void AdvanceWaypoint(int count)
    {
        _distanceToWaypoint = 0f;
        if (_waypointIndex == count - 1)
        {
            _lapCount++;
        }
        _waypointIndex = (_waypointIndex + 1) % count;
    }

    private string NextWaypointName(Transform[] points)
    {
        Transform next = _waypointIndex < points.Length ? points[_waypointIndex] : null;
        return next != null ? next.name : "(null)";
    }

    private string DescribeRoute()
    {
        Transform[] points = RoutePoints();
        string path = "";
        for (int i = 0; i < points.Length; i++)
        {
            path += (i > 0 ? " -> " : "") + (points[i] != null ? points[i].name : "(null)");
        }
        return path;
    }

    // Always-on route preview in the Scene view (cyan): point markers, the path
    // from each point to the next, and the loop back from the last to the first.
    private void OnDrawGizmos()
    {
        Transform[] points = RoutePoints();
        if (points.Length == 0)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] == null)
            {
                continue;
            }

            Gizmos.DrawWireSphere(points[i].position, arrivalThreshold);

            if (points.Length > 1)
            {
                Transform next = points[(i + 1) % points.Length];
                if (next != null)
                {
                    Gizmos.DrawLine(points[i].position, next.position);
                }
            }
        }

        // Detective → current target (also runs during Play).
        if (Application.isPlaying && points.Length > 0)
        {
            int current = _waypointIndex % points.Length;
            if (points[current] != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, points[current].position);
            }
        }
    }
}
