using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Settings")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioClip bgmClip;

    private void Awake()
    {
        // Cegah duplikasi AudioManager saat berpindah scene
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Menjaga objek ini tetap hidup antar scene

        InitAndPlayBGM();
    }

    private void InitAndPlayBGM()
    {
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
        }

        if (bgmClip != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.loop = true; // Otomatis mengulang lagu dari awal saat selesai
            bgmSource.playOnAwake = false;
            bgmSource.Play();
        }
    }
}