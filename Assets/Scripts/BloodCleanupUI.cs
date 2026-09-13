using UnityEngine;
using UnityEngine.UI;

public class BloodCleanupUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private GameObject canvasContainer;

    private Evidence _myEvidence;
    private JanitorCleanup _janitorCleanup;

    private void Awake()
    {
        _myEvidence = GetComponentInParent<Evidence>();
        // Mencari komponen JanitorCleanup di scene
        _janitorCleanup = Object.FindFirstObjectByType<JanitorCleanup>();
    }

    private void Update()
    {
        if (_janitorCleanup == null || fillImage == null || _myEvidence == null) return;

        // Cek apakah Janitor sedang membersihkan dan objek darah ini adalah targetnya
        bool isBeingCleaned = _janitorCleanup.IsCleaning && _janitorCleanup.Target == _myEvidence;

        if (isBeingCleaned)
        {
            if (canvasContainer != null && !canvasContainer.activeSelf)
            {
                canvasContainer.SetActive(true);
            }

            // Progress berjalan dari 0.0 (kosong) menuju 1.0 (penuh)
            fillImage.fillAmount = _janitorCleanup.CleaningProgress / _janitorCleanup.CleaningDuration;
        }
        else
        {
            if (canvasContainer != null && canvasContainer.activeSelf)
            {
                canvasContainer.SetActive(false);
            }
        }
    }
}