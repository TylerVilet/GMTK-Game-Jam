using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class DeathScreenUI : MonoBehaviour
{
    public GameObject panel;
    public Button restartButton;
    public TMP_Text scoreText;
    public TMP_Text highscoreText;

    public static DeathScreenUI Instance { get; private set; }


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        panel.SetActive(false);
    }


    public void ShowDeathScreen()
    {
        Debug.Log("show death screen");

        // update score and highscore
        Debug.Log(ScoreManager.Instance.highscore);
        Debug.Log(ScoreManager.Instance.score);

        highscoreText.text = "Highscore: " + ScoreManager.Instance.highscore;
        scoreText.text = "Score: " + ScoreManager.Instance.score;



        if (ScoreManager.Instance != null && scoreText != null)
            scoreText.text = "Score: " + ScoreManager.Instance.score;

        panel.SetActive(true);
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        // Reload GameScene specifically (not MainMenu) - WaveManager lives here
        // and its own Awake() re-loads MainMenu additively, which is what
        // actually brings the start screen back.
        SceneManager.LoadScene("GameScene");
    }
}