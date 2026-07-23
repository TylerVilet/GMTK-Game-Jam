using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PowerUpUI : MonoBehaviour
{
    [System.Serializable]
    public class PowerUpOption
    {
        public string title;
        [TextArea] public string description;
        // TODO: once player stats exist, add whatever's needed to actually apply this
        // (e.g. a stat enum + amount) instead of just logging the pick.
    }

    public GameObject panel;
    public PowerUpOption[] powerUpPool;
    public Button[] choiceButtons;   // drag the buttons on the panel in here
    public TMP_Text[] choiceLabels;  // matching text per button, same order

    PowerUpOption[] currentChoices;

    void Start()
    {
        panel.SetActive(false);

        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveCleared.AddListener(ShowChoices);

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            int index = i; // capture for the closure
            choiceButtons[i].onClick.AddListener(() => PickChoice(index));
        }
    }

    void OnDestroy()
    {
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveCleared.RemoveListener(ShowChoices);
    }

    void ShowChoices()
    {
        if (powerUpPool == null || powerUpPool.Length == 0) return;

        currentChoices = new PowerUpOption[choiceButtons.Length];
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            PowerUpOption option = powerUpPool[Random.Range(0, powerUpPool.Length)];
            currentChoices[i] = option;

            if (i < choiceLabels.Length && choiceLabels[i] != null)
                choiceLabels[i].text = $"{option.title}\n{option.description}";
        }

        panel.SetActive(true);
    }

    void PickChoice(int index)
    {
        PowerUpOption chosen = currentChoices[index];
        Debug.Log($"[PowerUpUI] Picked: {chosen.title} (effect not wired up yet)");

        panel.SetActive(false);

        if (WaveManager.Instance != null)
            WaveManager.Instance.StartNextWave();
    }
}
