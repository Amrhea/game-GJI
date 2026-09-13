using UnityEngine;
using UnityEngine.UI;

public class KillerCooldownUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private GameObject canvasContainer;

    private KillerKill _killerKill;

    private void Awake()
    {
        _killerKill = GetComponentInParent<KillerKill>();
        if (_killerKill == null)
        {
            _killerKill = GetComponent<KillerKill>();
        }

        // Sembunyikan UI sejak awal game karena cooldown belum berjalan
        if (canvasContainer != null)
        {
            canvasContainer.SetActive(false);
        }
    }

    private void Update()
    {
        if (_killerKill == null || fillImage == null) return;

        if (_killerKill.IsOnCooldown)
        {
            // Munculkan UI hanya ketika sedang cooldown
            if (canvasContainer != null && !canvasContainer.activeSelf)
            {
                canvasContainer.SetActive(true);
            }

            // Memperbarui isi progress bar dari 0% ke 100%
            fillImage.fillAmount = 1f - (_killerKill.CooldownRemaining / _killerKill.KillCooldown);
        }
        else
        {
            // Sembunyikan UI begitu cooldown selesai dan siap kill lagi
            if (canvasContainer != null && canvasContainer.activeSelf)
            {
                canvasContainer.SetActive(false);
            }
        }
    }
}