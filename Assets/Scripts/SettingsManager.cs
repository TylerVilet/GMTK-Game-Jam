using UnityEngine;
using UnityEngine.InputSystem;

// Persists across scene loads/restarts and remembers choices between sessions via PlayerPrefs.
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    // A rebindable action's trigger - either a keyboard key or a mouse button,
    // since some actions (reflect) default to a mouse button while others
    // (dash) default to a key, and both should be rebindable to either kind.
    public readonly struct InputBinding
    {
        public readonly bool isMouseButton;
        public readonly Key key;             // meaningful when isMouseButton is false
        public readonly int mouseButtonIndex; // meaningful when isMouseButton is true - 0=left, 1=right, 2=middle

        InputBinding(bool isMouseButton, Key key, int mouseButtonIndex)
        {
            this.isMouseButton = isMouseButton;
            this.key = key;
            this.mouseButtonIndex = mouseButtonIndex;
        }

        public static InputBinding FromKey(Key key) => new InputBinding(false, key, 0);
        public static InputBinding FromMouseButton(int index) => new InputBinding(true, Key.None, index);

        public bool WasPressedThisFrame()
        {
            if (isMouseButton)
            {
                if (Mouse.current == null) return false;
                switch (mouseButtonIndex)
                {
                    case 0: return Mouse.current.leftButton.wasPressedThisFrame;
                    case 1: return Mouse.current.rightButton.wasPressedThisFrame;
                    case 2: return Mouse.current.middleButton.wasPressedThisFrame;
                    default: return false;
                }
            }
            return Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
        }

        public override string ToString()
        {
            if (isMouseButton)
            {
                switch (mouseButtonIndex)
                {
                    case 0: return "Left Click";
                    case 1: return "Right Click";
                    case 2: return "Middle Click";
                    default: return "Mouse";
                }
            }
            return key.ToString();
        }
    }

    const string MusicVolumeKey = "Settings_MusicVolume";
    const string SfxVolumeKey = "Settings_SfxVolume";
    const string DashBindingIsMouseKey = "Settings_DashBinding_IsMouse";
    const string DashBindingValueKey = "Settings_DashBinding_Value";
    const string ReflectBindingIsMouseKey = "Settings_ReflectBinding_IsMouse";
    const string ReflectBindingValueKey = "Settings_ReflectBinding_Value";

    public float MusicVolume { get; private set; } = 1f;
    public float SfxVolume { get; private set; } = 1f;
    public InputBinding DashBinding { get; private set; } = InputBinding.FromKey(Key.LeftShift);
    public InputBinding ReflectBinding { get; private set; } = InputBinding.FromMouseButton(1);

    // MainMenu has one placed in the scene, but GameScene doesn't - this makes
    // sure settings (and the in-game Settings panel) still work if the game is
    // entered directly at GameScene instead of through the main menu first.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        GameObject root = new GameObject("SettingsManager");
        DontDestroyOnLoad(root);
        root.AddComponent<SettingsManager>();
    }

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
        DashBinding = LoadBinding(DashBindingIsMouseKey, DashBindingValueKey, DashBinding);
        ReflectBinding = LoadBinding(ReflectBindingIsMouseKey, ReflectBindingValueKey, ReflectBinding);

        AudioListener.volume = SfxVolume;
    }

    static InputBinding LoadBinding(string isMouseKey, string valueKey, InputBinding fallback)
    {
        int fallbackIsMouse = fallback.isMouseButton ? 1 : 0;
        int fallbackValue = fallback.isMouseButton ? fallback.mouseButtonIndex : (int)fallback.key;

        bool isMouse = PlayerPrefs.GetInt(isMouseKey, fallbackIsMouse) == 1;
        int value = PlayerPrefs.GetInt(valueKey, fallbackValue);

        return isMouse ? InputBinding.FromMouseButton(value) : InputBinding.FromKey((Key)value);
    }

    static void SaveBinding(string isMouseKey, string valueKey, InputBinding binding)
    {
        PlayerPrefs.SetInt(isMouseKey, binding.isMouseButton ? 1 : 0);
        PlayerPrefs.SetInt(valueKey, binding.isMouseButton ? binding.mouseButtonIndex : (int)binding.key);
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

    public void SetDashBinding(InputBinding binding)
    {
        DashBinding = binding;
        SaveBinding(DashBindingIsMouseKey, DashBindingValueKey, binding);
    }

    public void SetReflectBinding(InputBinding binding)
    {
        ReflectBinding = binding;
        SaveBinding(ReflectBindingIsMouseKey, ReflectBindingValueKey, binding);
    }
}
