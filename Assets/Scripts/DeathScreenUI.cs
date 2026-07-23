using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class DeathScreenUI : MonoBehaviour
{
    public GameObject panel;
    public Button restartButton;

    private WaveManager subscribedWaveManager;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        GameObject root = new GameObject("DeathScreenUI");
        DontDestroyOnLoad(root);
        root.AddComponent<DeathScreenUI>();
    }

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (subscribedWaveManager != null)
            subscribedWaveManager.OnGameOver.RemoveListener(ShowDeathScreen);
    }

    void Start()
    {
        BuildUI();
        panel.SetActive(false);
        restartButton.onClick.AddListener(Restart);
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
            subscribedWaveManager.OnGameOver.RemoveListener(ShowDeathScreen);

        subscribedWaveManager = WaveManager.Instance;

        if (subscribedWaveManager != null)
            subscribedWaveManager.OnGameOver.AddListener(ShowDeathScreen);
    }

    void BuildUI()
    {
        GameObject canvasObject = new GameObject("DeathScreenCanvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // above the HP HUD and power-up picker

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObject.AddComponent<GraphicRaycaster>();

        panel = new GameObject("DeathPanel");
        panel.transform.SetParent(canvasObject.transform, false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.8f);

        GameObject titleObject = new GameObject("Title");
        titleObject.transform.SetParent(panel.transform, false);
        RectTransform titleRect = titleObject.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.6f);
        titleRect.anchorMax = new Vector2(0.5f, 0.6f);
        titleRect.sizeDelta = new Vector2(800f, 150f);
        titleRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI titleText = titleObject.AddComponent<TextMeshProUGUI>();
        titleText.text = "GAME OVER";
        titleText.fontSize = 72f;
        titleText.color = Color.red;
        titleText.alignment = TextAlignmentOptions.Center;

        GameObject buttonObject = new GameObject("RestartButton");
        buttonObject.transform.SetParent(panel.transform, false);
        RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.4f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.4f);
        buttonRect.sizeDelta = new Vector2(260f, 70f);
        buttonRect.anchoredPosition = Vector2.zero;

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.8f, 0.15f, 0.15f, 1f);

        restartButton = buttonObject.AddComponent<Button>();
        restartButton.targetGraphic = buttonImage;

        GameObject buttonLabelObject = new GameObject("Label");
        buttonLabelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = buttonLabelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI buttonLabel = buttonLabelObject.AddComponent<TextMeshProUGUI>();
        buttonLabel.text = "Restart";
        buttonLabel.fontSize = 32f;
        buttonLabel.color = Color.white;
        buttonLabel.alignment = TextAlignmentOptions.Center;
    }

    void ShowDeathScreen()
    {
        panel.SetActive(true);
    }

    void Restart()
    {
        Time.timeScale = 1f;
        // Reload GameScene specifically (not MainMenu) - WaveManager lives here
        // and its own Awake() re-loads MainMenu additively, which is what
        // actually brings the start screen back.
        SceneManager.LoadScene("GameScene");
    }
}
