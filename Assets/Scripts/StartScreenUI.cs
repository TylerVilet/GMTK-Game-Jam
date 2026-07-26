using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartScreenUI : MonoBehaviour
{
    [System.Serializable]
    public class SelectableOption
    {
        public string label;
        public Button button;
        public GameObject weaponPrefab; // NEW - drag AK prefab / Uzi prefab here
        public Sprite leftSprite; // character options only - facing left
        public Sprite rightSprite; // character options only - facing right
    }

    [Header("Main Page")]
    public GameObject mainPage;
    public Button characterButton;
    public TMP_Text characterButtonLabel;
    public Button weaponButton;
    public TMP_Text weaponButtonLabel;
    public Button startButton;
    public TMP_Text highScoreText; // NEW - drag a TMP text object in for "High Score: X"

    [Header("Character Page (currently just Astronaut)")]
    public GameObject characterPage;
    public SelectableOption[] characterOptions;

    [Header("Weapon Page (currently just Gun)")]
    public GameObject weaponPage;
    public SelectableOption[] weaponOptions;

    [Header("Overall")]
    public GameObject startScreenRoot; // hidden entirely once Start is pressed - Settings is Main-Menu-only

    int selectedCharacterIndex = -1;
    int selectedWeaponIndex = -1;

    public static class GameSelection
    {
        public static GameObject SelectedCharacterPrefab;
        public static GameObject SelectedWeaponPrefab;
        public static Sprite SelectedCharacterLeftSprite;
        public static Sprite SelectedCharacterRightSprite;
    }

    void Start()
    {
        ShowMainPage();
        characterButton.onClick.AddListener(() => ShowPage(characterPage));
        weaponButton.onClick.AddListener(() => ShowPage(weaponPage));
        SetupOptions(characterOptions, SelectCharacter);
        SetupOptions(weaponOptions, SelectWeapon);
        startButton.onClick.AddListener(BeginGame);
        RefreshStartButton();
        RefreshHighScoreDisplay(); // NEW

    }

    void RefreshHighScoreDisplay() // NEW
    {
        if (highScoreText == null) return;
        int highScore = PlayerPrefs.GetInt("Highscore", 0); // matches ScoreManager.HighscoreKey - this used to read a different-cased key ("HighScore") that ScoreManager never wrote to, so it always showed a stale value
        highScoreText.text = "High Score: " + highScore;
    }

    void SetupOptions(SelectableOption[] options, System.Action<int> onPicked)
    {
        for (int i = 0; i < options.Length; i++)
        {
            int index = i;
            options[i].button.onClick.AddListener(() => onPicked(index));
        }
    }

    void SelectCharacter(int index)
    {
        selectedCharacterIndex = index;
        characterButtonLabel.text = characterOptions[index].label;
        GameSelection.SelectedCharacterPrefab = characterOptions[index].weaponPrefab; // the field name is misleading here � it's actually the character prefab
        GameSelection.SelectedCharacterLeftSprite = characterOptions[index].leftSprite;
        GameSelection.SelectedCharacterRightSprite = characterOptions[index].rightSprite;
        ShowMainPage();
        RefreshStartButton();
    }

    void SelectWeapon(int index)
    {
        selectedWeaponIndex = index;
        weaponButtonLabel.text = weaponOptions[index].label;
        GameSelection.SelectedWeaponPrefab = weaponOptions[index].weaponPrefab;
        ShowMainPage();
        RefreshStartButton();
    }

    void ShowPage(GameObject page)
    {
        mainPage.SetActive(false);
        characterPage.SetActive(false);
        weaponPage.SetActive(false);
        page.SetActive(true);
    }

    void ShowMainPage()
    {
        mainPage.SetActive(true);
        characterPage.SetActive(false);
        weaponPage.SetActive(false);
    }

    void RefreshStartButton()
    {
        startButton.interactable = selectedCharacterIndex >= 0 && selectedWeaponIndex >= 0;
    }

    void BeginGame()
    {
        startScreenRoot.SetActive(false);
        if (WaveManager.Instance != null)
            WaveManager.Instance.BeginGame();
    }
}