using UnityEngine;

/// <summary>
/// Minimal role marker for the Serial Killer. Exists so the Detective's direct
/// killer detection (and later chase/catch) can identify the killer reliably
/// without depending on GameObject names, tags or layer numbers. This is the
/// smallest possible identification mechanism — not a role framework. Local
/// gameplay logic only; networking is handled separately by Abim.
/// </summary>
public class KillerIdentity : MonoBehaviour
{
}