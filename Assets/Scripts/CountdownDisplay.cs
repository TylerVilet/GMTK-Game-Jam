using UnityEngine;
using TMPro;

public class CountdownDisplay : MonoBehaviour
{
    public TMP_Text text;

    void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    public void UpdateCountdown(float secondsRemaining)
    {
        text.text = Mathf.CeilToInt(secondsRemaining).ToString();
    }
}