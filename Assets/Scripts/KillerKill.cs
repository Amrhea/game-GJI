using System;
using UnityEngine;

/// <summary>
/// The Killer's single weapon: a knife with a fixed kill range and cooldown.
/// Space triggers an attempt: the nearest alive target inside range dies and a
/// corpse + blood evidence are spawned. Failed attempts do nothing and do not
/// start the cooldown. The Killer has no corpse interaction (no drag/dispose).
/// </summary>
[RequireComponent(typeof(KillerInput))]
public class KillerKill : MonoBehaviour
{
    [SerializeField] private KillerInput input;

    [Header("Kill rules (baseline)")]
    [SerializeField] private float killRange = 1.5f;
    [SerializeField] private float killCooldown = 4f;
    [SerializeField] private LayerMask targetMask = ~0;

    [Header("Crime scene results")]
    [SerializeField] private GameObject corpsePrefab;
    [SerializeField] private GameObject bloodPrefab;

    public float KillRange => killRange;
    public float CooldownRemaining { get; private set; }
    public bool IsOnCooldown => CooldownRemaining > 0f;

    /// <summary>Description of the most recent kill attempt (for the debug HUD).</summary>
    public string LastAttemptResult { get; private set; } = "Ready";

    /// <summary>Raised after a successful kill. Argument: the target that died.</summary>
    public event Action<KillTarget> KillPerformed;

    /// <summary>Raised after a successful kill. Arguments: corpse object, blood object.</summary>
    public event Action<GameObject, GameObject> EvidenceCreated;

    private void OnEnable()
    {
        input.KillPressed += AttemptKill;
    }

    private void OnDisable()
    {
        input.KillPressed -= AttemptKill;
    }

    private void Update()
    {
        if (CooldownRemaining > 0f)
        {
            CooldownRemaining = Mathf.Max(0f, CooldownRemaining - Time.deltaTime);
        }
    }

    public void AttemptKill()
    {
        if (IsOnCooldown)
        {
            LastAttemptResult = $"Blocked: cooldown {CooldownRemaining:0.0}s";
            Debug.Log($"[KillerKill] {LastAttemptResult}");
            return;
        }

        KillTarget target = FindNearestAliveTarget();
        if (target == null)
        {
            LastAttemptResult = $"No valid target within {killRange:0.0}m";
            Debug.Log($"[KillerKill] {LastAttemptResult}");
            return;
        }

        if (!target.TryKill())
        {
            // Defensive: the target died earlier this frame via another path.
            LastAttemptResult = $"Failed: {target.name} already dead";
            Debug.Log($"[KillerKill] {LastAttemptResult}");
            return;
        }

        // Successful kill: only now does the cooldown start.
        CooldownRemaining = killCooldown;

        Vector2 killPosition = target.transform.position;
        GameObject corpse = Instantiate(corpsePrefab, killPosition, Quaternion.identity);
        GameObject blood = Instantiate(bloodPrefab, killPosition, Quaternion.identity);

        target.gameObject.SetActive(false);

        LastAttemptResult = $"Killed {target.name} (cooldown {killCooldown:0.0}s)";
        Debug.Log($"[KillerKill] {LastAttemptResult} — corpse + blood spawned");

        KillPerformed?.Invoke(target);
        EvidenceCreated?.Invoke(corpse, blood);
    }

    private KillTarget FindNearestAliveTarget()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, killRange, targetMask);
        KillTarget best = null;
        float bestSqrDistance = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            KillTarget target = hit.GetComponentInParent<KillTarget>();
            if (target == null || !target.IsAlive || target.transform.root == transform.root)
            {
                continue;
            }

            float sqrDistance = ((Vector2)target.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                best = target;
            }
        }

        return best;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, killRange);
    }
}
