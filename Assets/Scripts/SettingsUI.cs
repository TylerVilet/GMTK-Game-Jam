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

    [Header("Whichever of these are showing get hidden while Settings is open, then restored on close")]
    public GameObject[] pagesToHideWhileOpen;

    readonly List<GameObject> pagesWeHid = new List<GameObject>();
    bool listeningForKey = false;

    void Start()
    {
        panel.SetActive(false);

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

        musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
        rebindDashButton.onClick.AddListener(BeginRebind);
    }

    void OpenPanel()
    {
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
    }

    void ClosePanel()
    {
        panel.SetActive(false);

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

    void BeginRebind()
    {
        listeningForKey = true;
        if (dashKeyLabel != null)
            dashKeyLabel.text = "Press any key...";
    }

    void Update()
    {
        if (!listeningForKey && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (panel.activeSelf)
                ClosePanel();
            else
                OpenPanel();
        }

        if (!listeningForKey || Keyboard.current == null) return;

        foreach (KeyControl key in Keyboard.current.allKeys)
        {
            if (key.wasPressedThisFrame)
            {
                if (SettingsManager.Instance != null)
                    SettingsManager.Instance.SetDashKey(key.keyCode);

                listeningForKey = false;
                UpdateDashKeyLabel();
                break;
            }
        }
    }

    void UpdateDashKeyLabel()
    {
        if (dashKeyLabel == null) return;

        Key key = SettingsManager.Instance != null ? SettingsManager.Instance.DashKey : Key.LeftShift;
        dashKeyLabel.text = key.ToString();
    }
}
