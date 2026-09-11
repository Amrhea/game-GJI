using UnityEngine;

public enum EvidenceType
{
    Corpse,
    Blood,
}

/// <summary>
/// Marks a scene object as crime-scene evidence. Corpse and blood are separate
/// objects/states so the Janitor (clean/drag) and the Detective (search) can
/// treat them independently later. The Killer never interacts with these.
/// </summary>
public class Evidence : MonoBehaviour
{
    [SerializeField] private EvidenceType type;

    public EvidenceType Type => type;
}
