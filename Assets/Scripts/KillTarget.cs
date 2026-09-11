using System;
using UnityEngine;

/// <summary>
/// Minimal NPC target for the prototype. Tracks alive/dead state only and dies
/// exactly once. The scene consequences (corpse + blood evidence) are created by
/// the killer on a successful kill, keeping the target itself stateless about them.
/// </summary>
public class KillTarget : MonoBehaviour
{
    public bool IsAlive { get; private set; } = true;

    /// <summary>Raised when the target dies. Argument is the target that died.</summary>
    public event Action<KillTarget> Killed;

    /// <summary>Kills the target. Returns false if it was already dead.</summary>
    public bool TryKill()
    {
        if (!IsAlive)
        {
            return false;
        }

        IsAlive = false;
        Killed?.Invoke(this);
        return true;
    }
}
