using UnityEngine;
using UnityEngine.InputSystem;

// Persists across scene loads/restarts and remembers choices between sessions via PlayerPrefs.
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    const string MusicVolumeKey = "Settings_MusicVolume";
    const string SfxVolumeKey = "Settings_SfxVolume";
    const string DashKeyKey = "Settings_DashKey";

    public float MusicVolume { get; private set; } = 1f;
    public float SfxVolume { get; private set; } = 1f;
    public Key DashKey { get; private set; } = Key.LeftShift;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        DashKey = (Key)PlayerPrefs.GetInt(DashKeyKey, (int)Key.LeftShift);

        AudioListener.volume = SfxVolume;
    }

    public void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);

        if (MusicManager.Instance != null)
            MusicManager.Instance.SetVolume(MusicVolume);

        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
    }

    public void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        AudioListener.volume = SfxVolume;
        PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
    }

    public void SetDashKey(Key key)
    {
        DashKey = key;
        PlayerPrefs.SetInt(DashKeyKey, (int)key);
    }
}
