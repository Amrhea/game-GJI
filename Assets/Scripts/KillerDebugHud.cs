using UnityEngine;

/// <summary>
/// Minimal on-screen debug overlay to verify killer gameplay during the jam.
/// Safe to delete before shipping.
/// </summary>
public class KillerDebugHud : MonoBehaviour
{
    [SerializeField] private Stamina stamina;
    [SerializeField] private KillerMovement movement;
    [SerializeField] private KillerKill kill;

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10f, 10f, 340f, 280f), GUI.skin.box);

        GUILayout.Label("=== KILLER DEBUG ===");

        DrawBar("Stamina", stamina.Current, stamina.Max, stamina.IsExhausted ? Color.grey : Color.cyan);
        GUILayout.Label(stamina.IsExhausted
            ? "EXHAUSTED - sprint locked until recovered"
            : movement.IsSprinting ? "Sprinting" : "Walking");

        GUILayout.Space(8f);
        GUILayout.Label(kill.IsOnCooldown
            ? $"Knife cooldown: {kill.CooldownRemaining:0.00}s"
            : $"Knife: READY (Space = kill, range {kill.KillRange:0.0}m)");
        GUILayout.Label("Last attempt: " + kill.LastAttemptResult);

        GUILayout.EndArea();
    }

    private static void DrawBar(string label, float value, float max, Color color)
    {
        GUILayout.Label($"{label}: {value:0}/{max:0}");
        Rect rect = GUILayoutUtility.GetRect(0f, 18f, GUILayout.ExpandWidth(true));
        GUI.backgroundColor = color;
        GUI.Box(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(value / max), rect.height), GUIContent.none);
        GUI.backgroundColor = Color.white;
    }
}
