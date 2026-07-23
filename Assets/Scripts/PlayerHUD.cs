using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerHUD : MonoBehaviour
{
    public Vector2 padding = new Vector2(20f, -20f);
    public Vector2 panelSize = new Vector2(420f, 80f);
    public int fontSize = 32;

    private Player player;
    private Text hpText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        GameObject root = new GameObject("PlayerHUD");
        DontDestroyOnLoad(root);
        root.AddComponent<PlayerHUD>();
    }

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        BuildUI();
        FindPlayer();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindPlayer();
    }

    void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        player = playerObject != null ? playerObject.GetComponent<Player>() : FindFirstObjectByType<Player>();
    }

    void BuildUI()
    {
        GameObject canvasObject = new GameObject("PlayerHUDCanvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject panelObject = new GameObject("HealthPanel");
        panelObject.transform.SetParent(canvasObject.transform, false);

        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = padding;
        panelRect.sizeDelta = panelSize;

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = Color.red;

        GameObject textObject = new GameObject("HealthText");
        textObject.transform.SetParent(panelObject.transform, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        hpText = textObject.AddComponent<Text>();
        hpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hpText.fontSize = fontSize;
        hpText.color = Color.white;
        hpText.alignment = TextAnchor.MiddleCenter;
    }

    void Update()
    {
        if (player == null || hpText == null)
        {
            return;
        }

        hpText.text = $"{Mathf.CeilToInt(player.health)}/{Mathf.CeilToInt(player.maxHealth)} HP";
    }
}
