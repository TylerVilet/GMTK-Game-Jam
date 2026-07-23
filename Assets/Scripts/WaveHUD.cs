using UnityEngine;
using TMPro;

public class WaveHUD : MonoBehaviour
{
    public TMP_Text waveText;
    public TMP_Text countdownText;
    public TMP_Text enemiesLeftText;

    void Start()
    {
        var wm = WaveManager.Instance;
        if (wm == null) return;

        wm.OnWaveStarted.AddListener(HandleWaveStarted);
        wm.OnCountdownChanged.AddListener(HandleCountdownChanged);
        wm.OnEnemiesLeftChanged.AddListener(HandleEnemiesLeftChanged);
    }

    void OnDestroy()
    {
        var wm = WaveManager.Instance;
        if (wm == null) return;

        wm.OnWaveStarted.RemoveListener(HandleWaveStarted);
        wm.OnCountdownChanged.RemoveListener(HandleCountdownChanged);
        wm.OnEnemiesLeftChanged.RemoveListener(HandleEnemiesLeftChanged);
    }

    void HandleWaveStarted(int waveIndex)
    {
        if (waveText != null) waveText.text = $"Wave {waveIndex + 1}";
    }

    void HandleCountdownChanged(float secondsLeft)
    {
        if (countdownText != null) countdownText.text = Mathf.CeilToInt(secondsLeft).ToString();
    }

    void HandleEnemiesLeftChanged(int remaining, int total)
    {
        if (enemiesLeftText != null) enemiesLeftText.text = $"{remaining} / {total} left";
    }
}
