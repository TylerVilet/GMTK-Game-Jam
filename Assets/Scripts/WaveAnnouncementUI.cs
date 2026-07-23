using UnityEngine;
using TMPro;
using System.Collections;

public class WaveAnnouncementUI : MonoBehaviour
{
    public TMP_Text text;
    public float displayDuration = 2f;

    private Coroutine currentAnnouncement;

    void Awake()
    {
        if (text == null)
            text = GetComponent<TMP_Text>();

        text.text = ""; // just clear the text, don't disable the GameObject
    }

    public void ShowWaveNumber(int waveIndex)
    {
        text.text = $"WAVE {waveIndex + 1} STARTING"; // +1 since CurrentWaveIndex is 0-based

        if (currentAnnouncement != null)
            StopCoroutine(currentAnnouncement);

        currentAnnouncement = StartCoroutine(ShowThenHide());
    }

    IEnumerator ShowThenHide()
    {
        yield return new WaitForSeconds(displayDuration);
        text.text = "";
    }
}