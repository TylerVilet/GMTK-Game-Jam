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

    // Set the moment this run's score first overtakes the previous highscore
    // (see UpdateScoreText) - lets SaveHighScoreIfBeaten know, once, at game
    // over, whether THIS run set a new record, without re-deriving it from
    // score/highscore (which are already equal by then since UpdateScoreText
    // keeps highscore synced live as score climbs).
    bool newHighscoreThisRun = false;

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
        newHighscoreThisRun = false;
        UpdateScoreText();
    }

    public void UpdateScoreText()
    {
        scoreText.text = "Score: " + score;

        if (score > highscore)
        {
            highscore = score;
            newHighscoreThisRun = true;
            SaveHighscore();
        }

        highscoreText.text = "Highscore: " + highscore;
    }

    void SaveHighscore()
    {
        PlayerPrefs.SetInt(HighscoreKey, highscore);
        PlayerPrefs.Save(); // force an immediate disk write rather than waiting for Unity's internal flush
    }

    // Called once from WaveManager.GameOver() - submits this run's score to
    // the global leaderboard, but only if it's a genuine new personal best,
    // so a losing/average run never spams the leaderboard.
    public void SaveHighScoreIfBeaten()
    {
        if (!newHighscoreThisRun) return;

        if (LeaderboardManager.Instance != null)
            LeaderboardManager.Instance.SubmitScore(highscore);
    }
}