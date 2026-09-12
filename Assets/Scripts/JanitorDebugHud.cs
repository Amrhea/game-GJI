using UnityEngine;

/// <summary>
/// Minimal on-screen debug overlay for Janitor prototype testing.
/// Safe to delete before shipping.
/// </summary>
public class JanitorDebugHud : MonoBehaviour
{
    [SerializeField] private JanitorMovement movement;
    [SerializeField] private JanitorCleanup cleanup;
    [SerializeField] private JanitorCorpseDrag corpseDrag;
    [SerializeField] private JanitorLightSwitch lightSwitch;

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10f, 10f, 360f, 200f), GUI.skin.box);

        GUILayout.Label("=== JANITOR DEBUG ===");

        bool dragging = corpseDrag != null && corpseDrag.IsDragging;
        bool cleaning = cleanup != null && cleanup.IsCleaning;
        string state = dragging
            ? "DRAGGING"
            : cleaning
                ? "CLEANING"
                : movement != null && movement.IsMovementLocked
                    ? "LOCKED"
                    : "MOVING";
        GUILayout.Label("State: " + state);

        if (dragging)
        {
            GUILayout.Label($"Drag speed: {corpseDrag.DragSpeed:0.0} m/s  (E to drop)");
        }
        else if (cleaning)
        {
            GUILayout.Label($"Cleaning: {cleanup.CleaningProgress:0.0} / {cleanup.CleaningDuration:0.0} s  (move to cancel)");
        }
        else
        {
            GUILayout.Label(cleanup != null
                ? $"E near Blood: clean (range {cleanup.InteractionRange:0.00} m) | E near Corpse: drag (range {(corpseDrag != null ? corpseDrag.InteractionRange : 0f):0.00} m)"
                : "Interact: no cleanup component");
        }

        if (lightSwitch != null)
        {
            GUILayout.Label($"Lights: {JanitorLamp.ActiveCount}/{JanitorLamp.TotalCount} ON (min {JanitorLamp.MinimumActiveCount})");
            JanitorLamp lamp = lightSwitch.FindNearestLamp();
            if (lamp == null)
            {
                GUILayout.Label("Nearest Lamp: NO");
            }
            else if (!lamp.IsOn)
            {
                GUILayout.Label($"Lamp OFF: {lamp.OffRemaining:0.0}s remaining");
            }
            else
            {
                GUILayout.Label($"Nearest Lamp: YES (E to turn off, range {lightSwitch.InteractionRange:0.00} m)");
            }
        }

        GUILayout.EndArea();
    }
}
