using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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
    public PowerUpOption[] powerUpPool = new PowerUpOption[]
    {
        new PowerUpOption { title = "Adrenaline Rush", description = "+20% move speed" },
        new PowerUpOption { title = "Reinforced Suit", description = "+10 max HP" },
        new PowerUpOption { title = "Rapid Fire", description = "+25% fire rate" },
        new PowerUpOption { title = "Heavy Rounds", description = "+30% bullet damage" },
        new PowerUpOption { title = "Long Barrel", description = "+50% bullet range" },
    };
    public Button[] choiceButtons;
    public TMP_Text[] choiceLabels;

    PowerUpOption[] currentChoices;
    private WaveManager subscribedWaveManager;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        GameObject root = new GameObject("PowerUpUI");
        DontDestroyOnLoad(root);
        root.AddComponent<PowerUpUI>();
    }

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (subscribedWaveManager != null)
            subscribedWaveManager.OnWaveCleared.RemoveListener(ShowChoices);
    }

    void Start()
    {
        BuildUI();
        panel.SetActive(false);

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            int index = i; // capture for the closure
            choiceButtons[i].onClick.AddListener(() => PickChoice(index));
        }

        SubscribeToWaveManager();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        panel.SetActive(false);
        SubscribeToWaveManager();
    }

    void SubscribeToWaveManager()
    {
        if (subscribedWaveManager != null)
            subscribedWaveManager.OnWaveCleared.RemoveListener(ShowChoices);

        subscribedWaveManager = WaveManager.Instance;

        if (subscribedWaveManager != null)
            subscribedWaveManager.OnWaveCleared.AddListener(ShowChoices);
    }

    void BuildUI()
    {
        GameObject canvasObject = new GameObject("PowerUpCanvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90; // above the HP HUD, below the death screen

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObject.AddComponent<GraphicRaycaster>();

        panel = new GameObject("PowerUpPanel");
        panel.transform.SetParent(canvasObject.transform, false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.75f);

        GameObject titleObject = new GameObject("Title");
        titleObject.transform.SetParent(panel.transform, false);
        RectTransform titleRect = titleObject.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.75f);
        titleRect.anchorMax = new Vector2(0.5f, 0.75f);
        titleRect.sizeDelta = new Vector2(900f, 120f);
        titleRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI titleText = titleObject.AddComponent<TextMeshProUGUI>();
        titleText.text = "WAVE CLEARED - CHOOSE AN UPGRADE";
        titleText.fontSize = 48f;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;

        int choiceCount = 3;
        choiceButtons = new Button[choiceCount];
        choiceLabels = new TMP_Text[choiceCount];

        float buttonWidth = 380f;
        float spacing = 40f;
        float totalWidth = choiceCount * buttonWidth + (choiceCount - 1) * spacing;
        float startX = -totalWidth * 0.5f + buttonWidth * 0.5f;

        for (int i = 0; i < choiceCount; i++)
        {
            GameObject buttonObject = new GameObject($"Choice{i}");
            buttonObject.transform.SetParent(panel.transform, false);

            RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.45f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.45f);
            buttonRect.sizeDelta = new Vector2(buttonWidth, 220f);
            buttonRect.anchoredPosition = new Vector2(startX + i * (buttonWidth + spacing), 0f);

            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.15f, 0.15f, 0.2f, 1f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            choiceButtons[i] = button;

            GameObject labelObject = new GameObject("Label");
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = labelObject.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(16f, 16f);
            labelRect.offsetMax = new Vector2(-16f, -16f);

            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = 26f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            choiceLabels[i] = label;
        }
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
