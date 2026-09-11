using System;
using UnityEngine;

/// <summary>
/// Simple sprint stamina for the Killer: one value with drain/recovery rules.
/// Deliberately not a framework — Tick() is driven by the movement component so
/// drain/recovery run at the physics rate.
/// </summary>
public class Stamina : MonoBehaviour
{
    [Header("Baseline tuning")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float sprintDrainPerSecond = 25f;
    [SerializeField] private float recoveryPerSecond = 20f;
    [SerializeField] private float recoveryDelay = 0.75f;

    [Tooltip("After being fully drained, stamina must recover above this value before sprinting again (prevents 0-stamina on/off flicker).")]
    [SerializeField] private float emptyLockThreshold = 10f;

    public float Current { get; private set; }
    public float Max => maxStamina;

    /// <summary>True right after stamina hit zero; cleared once stamina recovers above emptyLockThreshold.</summary>
    public bool IsExhausted { get; private set; }

    public event Action<float> StaminaChanged;
    public event Action BecameExhausted;
    public event Action RecoveredFromExhaustion;

    private float _timeSinceLastDrain;

    private void Awake()
    {
        Current = maxStamina;
    }

    /// <summary>True when sprinting (and therefore draining) is currently allowed.</summary>
    public bool CanSprint()
    {
        return !IsExhausted && Current > 0f;
    }

    /// <summary>
    /// Advance the stamina simulation.
    /// </summary>
    /// <param name="sprintRequested">Sprint button held.</param>
    /// <param name="isMoving">Whether the character is actually moving.</param>
    /// <param name="deltaTime">Delta time of the caller.</param>
    public void Tick(bool sprintRequested, bool isMoving, float deltaTime)
    {
        float previous = Current;
        bool wasExhausted = IsExhausted;

        bool sprinting = sprintRequested && isMoving && CanSprint();

        if (sprinting)
        {
            Current = Mathf.Max(0f, Current - sprintDrainPerSecond * deltaTime);
            _timeSinceLastDrain = 0f;
        }
        else
        {
            _timeSinceLastDrain += deltaTime;
            if (_timeSinceLastDrain >= recoveryDelay && Current < maxStamina)
            {
                Current = Mathf.Min(maxStamina, Current + recoveryPerSecond * deltaTime);
            }
        }

        if (!IsExhausted && Current <= 0f)
        {
            IsExhausted = true;
        }
        else if (IsExhausted && Current >= emptyLockThreshold)
        {
            IsExhausted = false;
        }

        if (!Mathf.Approximately(previous, Current))
        {
            StaminaChanged?.Invoke(Current);
        }

        if (!wasExhausted && IsExhausted)
        {
            BecameExhausted?.Invoke();
        }
        else if (wasExhausted && !IsExhausted)
        {
            RecoveredFromExhaustion?.Invoke();
        }
    }
}
