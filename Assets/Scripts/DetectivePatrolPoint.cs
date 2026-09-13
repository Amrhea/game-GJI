using UnityEngine;

/// <summary>
/// Optional authoring marker for a Detective patrol point.
/// Drop it on a child of the Detective's "Patrol Route" parent (the object wired
/// into DetectivePatrol.patrolRoute) so the point is easy to spot in the Hierarchy
/// and Scene view. Child order defines the patrol order — the route is read live
/// by DetectivePatrol. This component has no gameplay logic.
/// </summary>
public class DetectivePatrolPoint : MonoBehaviour
{
    // Scene-view marker so designers can spot patrol points at a glance.
    // Route lines / the loop-back segment are drawn by DetectivePatrol.OnDrawGizmos.
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.45f);
    }
}