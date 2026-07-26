using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class LeaderboardUI : MonoBehaviour
{
    [Header("Leaderboard panel")]
    public Button openButton;
    public GameObject panel;
    public Button closeButton;
    public TMP_Text listText;

    [Header("Blocked while the leaderboard is open, so Settings can't open on top of it")]
    public Button settingsOpenButton;

    void Start()
    {
        panel.SetActive(false);

        openButton.onClick.AddListener(OpenPanel);
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
    }

    void Update()
    {
        if (panel.activeSelf && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            ClosePanel();
    }

    void OpenPanel()
    {
        panel.SetActive(true);
        if (settingsOpenButton != null)
            settingsOpenButton.interactable = false;

        if (listText != null)
            listText.text = "Loading...";

        if (LeaderboardManager.Instance != null)
            LeaderboardManager.Instance.FetchTopScores(10, OnScoresLoaded);
        else if (listText != null)
            listText.text = "Leaderboard unavailable.";
    }

    void ClosePanel()
    {
        panel.SetActive(false);
        if (settingsOpenButton != null)
            settingsOpenButton.interactable = true;
    }

    void OnScoresLoaded(List<LeaderboardManager.LeaderboardEntry> entries)
    {
        if (listText == null) return;

        if (entries == null || entries.Count == 0)
        {
            listText.text = "No scores yet - be the first!";
            return;
        }

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < entries.Count; i++)
        {
            sb.AppendLine($"{i + 1}. {entries[i].name} - {entries[i].score}");
        }
        listText.text = sb.ToString();
    }
}
