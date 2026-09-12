using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// A prototype lamp: a toggleable URP Light2D with a simple ON/OFF state.
/// Turning it OFF lasts offDuration seconds (baseline 15 s), then it restores
/// itself automatically — the Janitor does not need to stay nearby. A small
/// static registry lets the 30% minimum-active rule count every lamp in the
/// scene without introducing a manager component.
/// </summary>
public class JanitorLamp : MonoBehaviour
{
    /// <summary>BASELINE: seconds a lamp stays OFF before restoring itself.</summary>
    [SerializeField] private float offDuration = 15f;

    private static readonly List<JanitorLamp> _all = new List<JanitorLamp>();

    /// <summary>All lamps currently alive in the scene (self-registering).</summary>
    public static IReadOnlyList<JanitorLamp> All => _all;

    /// <summary>Total registered (alive) lamp count.</summary>
    public static int TotalCount => _all.Count;

    /// <summary>Lamps currently ON.</summary>
    public static int ActiveCount
    {
        get
        {
            int on = 0;
            foreach (JanitorLamp lamp in _all)
            {
                if (lamp != null && lamp.IsOn)
                {
                    on++;
                }
            }

            return on;
        }
    }

    /// <summary>Minimum lamps that must stay ON: Ceil(total * 0.30).</summary>
    public static int MinimumActiveCount => Mathf.CeilToInt(_all.Count * 0.30f);

    /// <summary>True while the lamp is lit.</summary>
    public bool IsOn { get; private set; } = true;

    /// <summary>Seconds left until an OFF lamp restores itself (0 while ON).</summary>
    public float OffRemaining { get; private set; }

    /// <summary>Baseline OFF duration (for HUD/logs).</summary>
    public float OffDuration => offDuration;

    private Light2D _light;

    private void Awake()
    {
        _light = GetComponent<Light2D>();
        if (_light == null)
        {
            _light = GetComponentInChildren<Light2D>();
        }

        if (_light == null)
        {
            Debug.LogWarning($"[JanitorLamp] {name} has no Light2D — state will toggle without a visible light.");
        }
    }

    private void OnEnable()
    {
        _all.Add(this);
    }

    private void OnDisable()
    {
        _all.Remove(this);
    }

    private void Update()
    {
        if (IsOn)
        {
            return;
        }

        OffRemaining -= Time.deltaTime;
        if (OffRemaining <= 0f)
        {
            Restore();
        }
    }

    /// <summary>
    /// Attempts to turn the lamp OFF. Fails when it is already OFF, or when
    /// turning it off would drop the active count below the 30% minimum.
    /// </summary>
    public bool TryTurnOff()
    {
        if (!IsOn)
        {
            return false;
        }

        if (ActiveCount <= MinimumActiveCount)
        {
            return false;
        }

        IsOn = false;
        OffRemaining = offDuration;
        if (_light != null)
        {
            _light.enabled = false;
        }

        return true;
    }

    private void Restore()
    {
        Debug.Log($"[JanitorLamp] {name} auto-restored — ON again (was OFF for {offDuration:0.0} s)");
        IsOn = true;
        OffRemaining = 0f;
        if (_light != null)
        {
            _light.enabled = true;
        }
    }
}
