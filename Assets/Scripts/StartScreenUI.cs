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
    }

    [Header("Main Page")]
    public GameObject mainPage;
    public Button characterButton;
    public TMP_Text characterButtonLabel;
    public Button weaponButton;
    public TMP_Text weaponButtonLabel;
    public Button startButton;

    [Header("Character Page (currently just Astronaut)")]
    public GameObject characterPage;
    public SelectableOption[] characterOptions;

    [Header("Weapon Page (currently just Gun)")]
    public GameObject weaponPage;
    public SelectableOption[] weaponOptions;

    [Header("Overall")]
    public GameObject startScreenRoot; // hidden entirely once Start is pressed

    int selectedCharacterIndex = -1;
    int selectedWeaponIndex = -1;

    void Start()
    {
        ShowMainPage();

        characterButton.onClick.AddListener(() => ShowPage(characterPage));
        weaponButton.onClick.AddListener(() => ShowPage(weaponPage));

        SetupOptions(characterOptions, SelectCharacter);
        SetupOptions(weaponOptions, SelectWeapon);

        startButton.onClick.AddListener(BeginGame);
        RefreshStartButton();
    }

    void SetupOptions(SelectableOption[] options, System.Action<int> onPicked)
    {
        for (int i = 0; i < options.Length; i++)
        {
            int index = i; // capture for the closure
            options[i].button.onClick.AddListener(() => onPicked(index));
        }
    }

    void SelectCharacter(int index)
    {
        selectedCharacterIndex = index;
        characterButtonLabel.text = characterOptions[index].label;
        ShowMainPage();
        RefreshStartButton();
    }

    void SelectWeapon(int index)
    {
        selectedWeaponIndex = index;
        weaponButtonLabel.text = weaponOptions[index].label;
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
