using TMPro;
using UnityEngine;
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;
    public int score = 0;
    public int highscore = 0;
    public TMP_Text scoreText;
    public TMP_Text highscoreText;

    const string HighscoreKey = "Highscore";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        highscore = PlayerPrefs.GetInt(HighscoreKey, 0); // load saved highscore, default 0 if none exists yet
        UpdateScoreText();
    }

    public void AddScore(int amount)
    {
        score += amount;
        Debug.Log("Score: " + score);
        UpdateScoreText();
    }

    public void ResetScore()
    {
        score = 0;
        UpdateScoreText();
    }

    public void UpdateScoreText()
    {
        scoreText.text = "Score: " + score;

        if (score > highscore)
        {
            highscore = score;
            SaveHighscore();
        }

        highscoreText.text = "Highscore: " + highscore;
    }

    void SaveHighscore()
    {
        PlayerPrefs.SetInt(HighscoreKey, highscore);
        PlayerPrefs.Save(); // force an immediate disk write rather than waiting for Unity's internal flush
    }
}