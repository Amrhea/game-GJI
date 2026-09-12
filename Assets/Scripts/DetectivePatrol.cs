using UnityEngine;

/// <summary>
/// Baseline Detective AI: patrols through a designer-defined list of patrol points
/// in order, then wraps back to the first point after the last one (A→B→C→D→A…).
/// Travels in straight lines via a kinematic Rigidbody2D — the same movement pattern
/// as KillerMovement. No NavMesh in this project; points must be placed with a clear
/// straight path between neighbours.
/// Patrol ONLY: no detection, suspicion, line of sight or chase yet.
/// Patrol can be externally paused/resumed (see DetectiveDetection).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class DetectivePatrol : MonoBehaviour
{
    [Header("Route (order matters; loop after the last point)")]
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

    /// <summary>Number of configured patrol points.</summary>
    public int WaypointCount => waypoints != null ? waypoints.Length : 0;

    /// <summary>Index of the point the detective currently travels toward.</summary>
    public int CurrentWaypointIndex => _waypointIndex;

    /// <summary>True while a usable route exists (at least one non-null waypoint).</summary>
    public bool HasRoute =>
        WaypointCount > 0 && System.Array.Exists(waypoints, w => w != null);

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
        if (!HasRoute)
        {
            if (!_warnedNoRoute)
            {
                _warnedNoRoute = true;
                Debug.LogWarning($"[{nameof(DetectivePatrol)}] No waypoints assigned — detective stands still.");
            }
            return;
        }

        if (_paused)
        {
            return;
        }

        Transform waypoint = waypoints[_waypointIndex];
        if (waypoint == null)
        {
            // Designer cleared/removed this point: skip it instead of freezing.
            AdvanceWaypoint();
            return;
        }

        Vector2 position = _body.position;
        Vector2 toWaypoint = (Vector2)waypoint.position - position;
        _distanceToWaypoint = toWaypoint.magnitude;

        if (_distanceToWaypoint <= arrivalThreshold)
        {
            string reached = waypoint.name;
            int reachedPosition = _waypointIndex + 1;
            bool wrapped = _waypointIndex == WaypointCount - 1;
            AdvanceWaypoint();
            Debug.Log($"[DetectivePatrol] Reached {reached} (#{reachedPosition}/{WaypointCount}) — next: {NextWaypointName()}" +
                      (wrapped ? $"  | lap #{_lapCount} complete" : ""));
            return;
        }

        // Clamp the step so the detective never overshoots the point.
        float step = Mathf.Min(patrolSpeed * Time.fixedDeltaTime, _distanceToWaypoint);
        _body.MovePosition(position + toWaypoint / _distanceToWaypoint * step);
    }

    private void AdvanceWaypoint()
    {
        _distanceToWaypoint = 0f;
        if (_waypointIndex == WaypointCount - 1)
        {
            _lapCount++;
        }
        _waypointIndex = (_waypointIndex + 1) % WaypointCount;
    }

    private string NextWaypointName()
    {
        Transform next = waypoints[_waypointIndex];
        return next != null ? next.name : "(null)";
    }

    private string DescribeRoute()
    {
        string path = "";
        for (int i = 0; i < WaypointCount; i++)
        {
            Transform waypoint = waypoints[i];
            path += (i > 0 ? " -> " : "") + (waypoint != null ? waypoint.name : "(null)");
        }
        return path;
    }

    private void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            return;
        }

        // Route line A→B→…→A.
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null)
            {
                continue;
            }

            Gizmos.DrawWireSphere(waypoints[i].position, arrivalThreshold);

            if (waypoints.Length > 1)
            {
                Transform next = waypoints[(i + 1) % waypoints.Length];
                if (next != null)
                {
                    Gizmos.DrawLine(waypoints[i].position, next.position);
                }
            }
        }

        // Detective → current target while selected.
        if (Application.isPlaying && waypoints[_waypointIndex] != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, waypoints[_waypointIndex].position);
        }
    }
}
