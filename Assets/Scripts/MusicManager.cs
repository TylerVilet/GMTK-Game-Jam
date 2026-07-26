using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip gameplayTrack;
    [SerializeField][Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private bool playOnStart = true;

    // The music was blasting at full AudioSource volume when the settings slider
    // read 100% - this scales actual output down so the slider's 100% only ever
    // reaches 30% of the old max loudness, without changing what the slider/UI shows.
    const float MaxVolumeScale = 0.3f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (musicSource == null) return;

        if (SettingsManager.Instance != null)
            volume = SettingsManager.Instance.MusicVolume;

        musicSource.clip = gameplayTrack;
        musicSource.loop = true;
        musicSource.volume = volume * MaxVolumeScale;

        if (playOnStart)
            musicSource.Play();
    }

    public void Play()
    {
        if (musicSource == null) return;

        if (!musicSource.isPlaying)
            musicSource.Play();
    }

    public void Stop()
    {
        if (musicSource == null) return;

        musicSource.Stop();
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        if (musicSource != null)
            musicSource.volume = volume * MaxVolumeScale;
    }
}