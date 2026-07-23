using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DeathScreenUI : MonoBehaviour
{
    public GameObject panel;
    public Button restartButton;

    void Start()
    {
        panel.SetActive(false);

        if (WaveManager.Instance != null)
            WaveManager.Instance.OnGameOver.AddListener(ShowDeathScreen);

        restartButton.onClick.AddListener(Restart);
    }

    void OnDestroy()
    {
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnGameOver.RemoveListener(ShowDeathScreen);
    }

    void ShowDeathScreen()
    {
        panel.SetActive(true);
    }

    void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
