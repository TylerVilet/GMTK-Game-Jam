using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip gameplayTrack;
    [SerializeField][Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private bool playOnStart = true;

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
        musicSource.volume = volume;

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
            musicSource.volume = volume;
    }
}