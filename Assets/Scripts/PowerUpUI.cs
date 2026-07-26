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
        [TextArea] public string descriptionTemplate; // use {0} where the rolled value should appear, e.g. "+{0}% move speed"
        public PowerUpType type;
        // Possible integer values to randomly roll from each time this shows up.
        // For percentage-based types, these are percentages (e.g. 15, 20, 25 -> +15%/+20%/+25%).
        // For MaxHealth, these are flat HP amounts (e.g. 8, 10, 12).
        public int[] possibleValues;
        public Sprite icon;
    }

    public GameObject panel;
    public PowerUpOption[] powerUpPool = new PowerUpOption[]
    {
        new PowerUpOption { title = "Adrenaline Rush", descriptionTemplate = "+{0}% move speed", type = PowerUpType.MoveSpeed, possibleValues = new int[] { 20, 25, 30 } },
        new PowerUpOption { title = "Reinforced Suit", descriptionTemplate = "+{0} max HP", type = PowerUpType.MaxHealth, possibleValues = new int[] {  10, 15, 20 } },
        new PowerUpOption { title = "Rapid Fire", descriptionTemplate = "+{0}% fire rate", type = PowerUpType.FireRate, possibleValues = new int[] { 15, 20, 25 } },
        new PowerUpOption { title = "Heavy Rounds", descriptionTemplate = "+{0}% bullet damage", type = PowerUpType.Damage, possibleValues = new int[] { 15, 20, 25 } },
        new PowerUpOption { title = "Long Barrel", descriptionTemplate = "+{0}% bullet range", type = PowerUpType.BulletRange, possibleValues = new int[] { 40, 45, 50 } },
        new PowerUpOption { title = "Lengthy Dash", descriptionTemplate = "+{0}% distance for dash", type = PowerUpType.DashDistance, possibleValues = new int[] { 30, 40, 50 } },
        new PowerUpOption { title = "Quick Dash", descriptionTemplate = "+{0}% speed for dash", type = PowerUpType.DashSpeed, possibleValues = new int[] { 30, 40, 50 } },
        new PowerUpOption { title = "Heal Wounds", descriptionTemplate = "restore player health to max", type = PowerUpType.FullHealth, possibleValues = new int[] { 100 } },
    };
    public Button[] choiceButtons;
    public TMP_Text[] choiceTitles;
    public TMP_Text[] choiceDescriptions;
    public Image[] choiceImages;

    PowerUpOption[] currentChoices;
    int[] currentChoiceValues; // the rolled value for each currently-shown choice, parallel to currentChoices
    private WaveManager subscribedWaveManager;

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

    // Rolls a random value from the option's possibleValues array.
    int RollValue(PowerUpOption option)
    {
        if (option.possibleValues == null || option.possibleValues.Length == 0)
            return 0;

        return option.possibleValues[Random.Range(0, option.possibleValues.Length)];
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
        currentChoiceValues = new int[choiceButtons.Length];

        for (int i = 0; i < choiceCount; i++)
        {
            int pickIndex = Random.Range(0, availablePool.Count);
            PowerUpOption option = availablePool[pickIndex];
            availablePool.RemoveAt(pickIndex); // no repeats within this set of choices

            currentChoices[i] = option;

            // Roll a random value for this option (e.g. 15/20/25%) fresh each time it's shown
            int rolledValue = RollValue(option);
            currentChoiceValues[i] = rolledValue;

            if (i < choiceTitles.Length && choiceTitles[i] != null)
                choiceTitles[i].text = option.title;

            if (i < choiceDescriptions.Length && choiceDescriptions[i] != null)
            {
                string desc = string.IsNullOrEmpty(option.descriptionTemplate)
                    ? ""
                    : string.Format(option.descriptionTemplate, rolledValue);
                choiceDescriptions[i].text = desc;
            }

            if (i < choiceImages.Length && choiceImages[i] != null)
            {
                choiceImages[i].sprite = option.icon;
                choiceImages[i].enabled = option.icon != null;
            }
        }

        // In case availablePool ran out before filling all buttons (e.g. FullHealth
        // excluded and pool is small), disable any leftover buttons/labels
        for (int i = choiceCount; i < choiceButtons.Length; i++)
        {
            currentChoices[i] = null;
            currentChoiceValues[i] = 0;
            if (choiceButtons[i] != null)
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
            else
            {
                if (i < choiceTitles.Length && choiceTitles[i] != null) choiceTitles[i].text = "";
                if (i < choiceDescriptions.Length && choiceDescriptions[i] != null) choiceDescriptions[i].text = "";
                if (i < choiceImages.Length && choiceImages[i] != null) choiceImages[i].enabled = false;
            }
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
        int rolledValue = currentChoiceValues[index];
        ApplyPowerUp(chosen, rolledValue);

        panel.SetActive(false);

        if (WaveManager.Instance != null)
            WaveManager.Instance.StartNextWave();
    }

    void ApplyPowerUp(PowerUpOption option, int rolledValue)
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        Player player = playerObject != null ? playerObject.GetComponent<Player>() : FindFirstObjectByType<Player>();
        Gun gun = FindFirstObjectByType<Gun>();

        float fraction = rolledValue / 100f; // percent -> multiplier, e.g. 20 -> 0.2f

        switch (option.type)
        {
            case PowerUpType.MoveSpeed:
                if (player != null) player.speed *= 1f + fraction;
                break;
            case PowerUpType.MaxHealth:
                if (player != null)
                {
                    // MaxHealth uses the rolled value as a flat amount, not a percentage
                    player.maxHealth += rolledValue;
                    player.health += rolledValue;
                    player.updateHealthUI();
                }
                break;
            case PowerUpType.FireRate:
                if (gun != null) gun.IncreaseFireRate(fraction);
                break;
            case PowerUpType.Damage:
                if (gun != null) gun.damageMultiplier *= 1f + fraction;
                break;
            case PowerUpType.BulletRange:
                if (gun != null) gun.rangeMultiplier *= 1f + fraction;
                break;
            case PowerUpType.DashDistance:
                if (player != null) player.dashDistance *= 1f + fraction;
                break;
            case PowerUpType.DashSpeed:
                if (player != null) player.dashSpeed *= 1f + fraction;
                break;
            case PowerUpType.FullHealth:
                if (player != null)
                {
                    player.health = player.maxHealth;
                    player.updateHealthUI();
                }
                break;
        }

        Debug.Log($"[PowerUpUI] Applied: {option.title} ({rolledValue})");
    }
}