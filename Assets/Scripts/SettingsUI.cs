using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using TMPro;

public class SettingsUI : MonoBehaviour
{
    public GameObject panel;
    public Button openButton;
    public Button closeButton;
    public Slider musicSlider;
    public TMP_Text musicValueLabel;
    public Slider sfxSlider;
    public TMP_Text sfxValueLabel;
    public Button rebindDashButton;
    public TMP_Text dashKeyLabel;
    public Button rebindReflectButton;
    public TMP_Text reflectKeyLabel;

    [Header("Whichever of these are showing get hidden while Settings is open, then restored on close")]
    public GameObject[] pagesToHideWhileOpen;

    [Header("While this is open, Escape closes it instead of opening Settings")]
    public GameObject leaderboardPanel;

    enum RebindTarget { None, Dash, Reflect }

    readonly List<GameObject> pagesWeHid = new List<GameObject>();
    RebindTarget listeningFor = RebindTarget.None;
    int listenStartFrame = -1; // rebinding starts via a left-click, so we skip scanning until the next frame - otherwise that same click immediately self-binds to "Left Click"

    void Start()
    {
        panel.SetActive(false);
        Time.timeScale = 1f; // in case a scene loaded while a previous SettingsUI had paused it - timeScale isn't reset by a scene load on its own

        openButton.onClick.AddListener(OpenPanel);
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        if (SettingsManager.Instance != null)
        {
            musicSlider.value = SettingsManager.Instance.MusicVolume;
            sfxSlider.value = SettingsManager.Instance.SfxVolume;
        }
        UpdateValueLabel(musicValueLabel, musicSlider.value);
        UpdateValueLabel(sfxValueLabel, sfxSlider.value);
        UpdateDashKeyLabel();
        UpdateReflectKeyLabel();

        musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
        rebindDashButton.onClick.AddListener(() => BeginRebind(RebindTarget.Dash));
        if (rebindReflectButton != null)
            rebindReflectButton.onClick.AddListener(() => BeginRebind(RebindTarget.Reflect));
    }

    void OpenPanel()
    {
        if (panel.activeSelf) return; // already open - re-scanning here would find everything already hidden and wipe pagesWeHid, losing track of what to restore on close

        pagesWeHid.Clear();
        if (pagesToHideWhileOpen != null)
        {
            foreach (var page in pagesToHideWhileOpen)
            {
                if (page != null && page.activeSelf)
                {
                    pagesWeHid.Add(page);
                    page.SetActive(false);
                }
            }
        }

        panel.SetActive(true);
        Time.timeScale = 0f; // pause gameplay while settings is open - input/UI still work since those aren't time-scaled
    }

    void ClosePanel()
    {
        panel.SetActive(false);
        Time.timeScale = 1f;

        foreach (var page in pagesWeHid)
        {
            if (page != null) page.SetActive(true);
        }
        pagesWeHid.Clear();
    }

    void OnMusicSliderChanged(float value)
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.SetMusicVolume(value);

        UpdateValueLabel(musicValueLabel, value);
    }

    void OnSfxSliderChanged(float value)
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.SetSfxVolume(value);

        UpdateValueLabel(sfxValueLabel, value);
    }

    void UpdateValueLabel(TMP_Text label, float sliderValue)
    {
        if (label != null)
            label.text = Mathf.RoundToInt(sliderValue * 100f) + "%";
    }

    void BeginRebind(RebindTarget target)
    {
        listeningFor = target;
        listenStartFrame = Time.frameCount;

        TMP_Text label = target == RebindTarget.Dash ? dashKeyLabel : reflectKeyLabel;
        if (label != null)
            label.text = "Press any key...";
    }

    void Update()
    {
        bool leaderboardOpen = leaderboardPanel != null && leaderboardPanel.activeSelf;
        if (!leaderboardOpen && listeningFor == RebindTarget.None && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (panel.activeSelf)
                ClosePanel();
            else
                OpenPanel();
        }

        if (listeningFor == RebindTarget.None) return;
        if (Time.frameCount == listenStartFrame) return; // don't let the click that opened rebinding also complete it

        SettingsManager.InputBinding? picked = null;

        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame) picked = SettingsManager.InputBinding.FromMouseButton(0);
            else if (Mouse.current.rightButton.wasPressedThisFrame) picked = SettingsManager.InputBinding.FromMouseButton(1);
            else if (Mouse.current.middleButton.wasPressedThisFrame) picked = SettingsManager.InputBinding.FromMouseButton(2);
        }

        if (picked == null && Keyboard.current != null)
        {
            foreach (KeyControl key in Keyboard.current.allKeys)
            {
                if (key.wasPressedThisFrame)
                {
                    picked = SettingsManager.InputBinding.FromKey(key.keyCode);
                    break;
                }
            }
        }

        if (picked == null) return;

        if (SettingsManager.Instance != null)
        {
            if (listeningFor == RebindTarget.Dash) SettingsManager.Instance.SetDashBinding(picked.Value);
            else SettingsManager.Instance.SetReflectBinding(picked.Value);
        }

        listeningFor = RebindTarget.None;
        UpdateDashKeyLabel();
        UpdateReflectKeyLabel();
    }

    void UpdateDashKeyLabel()
    {
        if (dashKeyLabel == null) return;

        SettingsManager.InputBinding binding = SettingsManager.Instance != null
            ? SettingsManager.Instance.DashBinding
            : SettingsManager.InputBinding.FromKey(Key.LeftShift);
        dashKeyLabel.text = binding.ToString();
    }

    void UpdateReflectKeyLabel()
    {
        if (reflectKeyLabel == null) return;

        SettingsManager.InputBinding binding = SettingsManager.Instance != null
            ? SettingsManager.Instance.ReflectBinding
            : SettingsManager.InputBinding.FromMouseButton(1);
        reflectKeyLabel.text = binding.ToString();
    }
}
