using UnityEngine;
using UnityEngine.UI;

public class LampCooldownUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private GameObject canvasContainer;

    private JanitorLamp _lamp;

    private void Awake()
    {
        // Mengambil JanitorLamp di objek ini atau objek parent-nya
        _lamp = GetComponentInParent<JanitorLamp>();
    }

    private void Update()
    {
        if (_lamp == null || fillImage == null) return;

        // Cek jika lampu sedang MATI (!IsOn)
        if (!_lamp.IsOn)
        {
            if (canvasContainer != null && !canvasContainer.activeSelf) 
            {
                canvasContainer.SetActive(true);
            }

            // Membagi sisa waktu dengan total durasi (1.0 -> 0.0)
            fillImage.fillAmount = 1f - (_lamp.OffRemaining / _lamp.OffDuration);
        }
        else
        {
            // Sembunyikan progress bar jika lampu sedang NYALA
            if (canvasContainer != null && canvasContainer.activeSelf) 
            {
                canvasContainer.SetActive(false);
            }
        }
    }
}