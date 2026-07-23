using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PowerUpUI : MonoBehaviour
{
    public enum PowerUpType
    {
        MoveSpeed,
        MaxHealth,
        FireRate,
        Damage,
        BulletRange,
        DashDistance,
        DashSpeed,
        FullHealth
    }

    [System.Serializable]
    public class PowerUpOption
    {
        public string title;
        [TextArea] public string description;
        public PowerUpType type;
        // Fraction (0.2 = +20%) for the multiplicative stats, flat amount for MaxHealth.
        public float amount;
    }

    public GameObject panel;
    public PowerUpOption[] powerUpPool = new PowerUpOption[]
    {
        new PowerUpOption { title = "Adrenaline Rush", description = "+20% move speed", type = PowerUpType.MoveSpeed, amount = 0.2f },
        new PowerUpOption { title = "Reinforced Suit", description = "+10 max HP", type = PowerUpType.MaxHealth, amount = 10f },
        new PowerUpOption { title = "Rapid Fire", description = "+25% fire rate", type = PowerUpType.FireRate, amount = 0.25f },
        new PowerUpOption { title = "Heavy Rounds", description = "+30% bullet damage", type = PowerUpType.Damage, amount = 0.3f },
        new PowerUpOption { title = "Long Barrel", description = "+50% bullet range", type = PowerUpType.BulletRange, amount = 0.5f },
        new PowerUpOption { title = "Lengthy Dash", description = "+20% distance for dash", type = PowerUpType.DashDistance, amount = 0.2f },
        new PowerUpOption { title = "Quick Dash", description = "+20% speed for dash", type = PowerUpType.DashSpeed, amount = 0.2f },
        new PowerUpOption { title = "Heal Wounds", description = "restore player health to max", type = PowerUpType.FullHealth, amount = 1f },
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
        // panel.SetActive(false);
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

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        Player player = playerObject != null ? playerObject.GetComponent<Player>() : FindFirstObjectByType<Player>();

        bool isFullHealth = player != null && player.health >= player.maxHealth;

        // Build a pool that excludes FullHealth if the player doesn't need it
        System.Collections.Generic.List<PowerUpOption> availablePool = new System.Collections.Generic.List<PowerUpOption>();
        foreach (var option in powerUpPool)
        {
            if (option.type == PowerUpType.FullHealth && isFullHealth)
                continue;
            availablePool.Add(option);
        }

        if (availablePool.Count == 0) return;

        int choiceCount = Mathf.Min(choiceButtons.Length, availablePool.Count);
        currentChoices = new PowerUpOption[choiceButtons.Length];

        for (int i = 0; i < choiceCount; i++)
        {
            int pickIndex = Random.Range(0, availablePool.Count);
            PowerUpOption option = availablePool[pickIndex];
            availablePool.RemoveAt(pickIndex); // no repeats within this set of choices

            currentChoices[i] = option;

            if (i < choiceLabels.Length && choiceLabels[i] != null)
                choiceLabels[i].text = $"{option.title}\n{option.description}";
        }

        // In case availablePool ran out before filling all buttons (e.g. FullHealth
        // excluded and pool is small), disable any leftover buttons/labels
        for (int i = choiceCount; i < choiceButtons.Length; i++)
        {
            currentChoices[i] = null;
            if (choiceButtons[i] != null) choiceButtons[i].gameObject.SetActive(false);
            else if (i < choiceLabels.Length && choiceLabels[i] != null) choiceLabels[i].text = "";
        }
        for (int i = 0; i < choiceCount; i++)
        {
            if (choiceButtons[i] != null) choiceButtons[i].gameObject.SetActive(true);
        }

        panel.SetActive(true);
    }

    void PickChoice(int index)
    {
        if (currentChoices == null || index >= currentChoices.Length || currentChoices[index] == null)
            return;

        PowerUpOption chosen = currentChoices[index];
        ApplyPowerUp(chosen);

        panel.SetActive(false);

        if (WaveManager.Instance != null)
            WaveManager.Instance.StartNextWave();
    }

    void ApplyPowerUp(PowerUpOption option)
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        Player player = playerObject != null ? playerObject.GetComponent<Player>() : FindFirstObjectByType<Player>();
        Gun gun = FindFirstObjectByType<Gun>();

        switch (option.type)
        {
            case PowerUpType.MoveSpeed:
                if (player != null) player.speed *= 1f + option.amount;
                break;
            case PowerUpType.MaxHealth:
                if (player != null)
                {
                    player.maxHealth += option.amount;
                    player.health += option.amount;
                    player.updateHealthUI();
                }
                break;
            case PowerUpType.FireRate:
                if (gun != null) gun.IncreaseFireRate(option.amount);
                break;
            case PowerUpType.Damage:
                if (gun != null) gun.damageMultiplier *= 1f + option.amount;
                break;
            case PowerUpType.BulletRange:
                if (gun != null) gun.rangeMultiplier *= 1f + option.amount;
                break;
            case PowerUpType.DashDistance:
                if (player != null) player.dashDistance *= 1f + option.amount;
                break;
            case PowerUpType.DashSpeed:
                if (player != null) player.dashSpeed *= 1f + option.amount;
                break;
            case PowerUpType.FullHealth:
                if (player != null)
                {
                    player.health = player.maxHealth;
                    player.updateHealthUI();
                }
                break;
        }

        Debug.Log($"[PowerUpUI] Applied: {option.title}");
    }
}
