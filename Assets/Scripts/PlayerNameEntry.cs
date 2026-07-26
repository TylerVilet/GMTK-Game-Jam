using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

// In-game "Name:" HUD entry, sitting between the Score and Highscore text.
// Click it, type, press Enter to save - same capture pattern as the Settings
// rebind buttons, just for a whole name instead of a single key.
public class PlayerNameEntry : MonoBehaviour
{
    const int MaxNameLength = 16;

    public Button nameButton;
    public TMP_Text nameLabel;

    bool enteringName = false;
    string nameBuffer = "";

    void Start()
    {
        UpdateLabel();

        if (nameButton != null)
            nameButton.onClick.AddListener(BeginNameEntry);
    }

    void OnDestroy()
    {
        if (Keyboard.current != null)
            Keyboard.current.onTextInput -= OnTextInput;
    }

    void BeginNameEntry()
    {
        if (enteringName) return;

        enteringName = true;
        nameBuffer = "";
        RefreshEnteringLabel();

        if (Keyboard.current != null)
            Keyboard.current.onTextInput += OnTextInput;
    }

    void OnTextInput(char c)
    {
        if (!enteringName || char.IsControl(c)) return; // control chars (enter, etc.) are handled in Update instead

        if (nameBuffer.Length < MaxNameLength)
            nameBuffer += c;

        RefreshEnteringLabel();
    }

    void Update()
    {
        if (!enteringName || Keyboard.current == null) return;

        if (Keyboard.current.backspaceKey.wasPressedThisFrame && nameBuffer.Length > 0)
        {
            nameBuffer = nameBuffer.Substring(0, nameBuffer.Length - 1);
            RefreshEnteringLabel();
        }
        else if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
        {
            FinishNameEntry();
        }
    }

    void RefreshEnteringLabel()
    {
        if (nameLabel != null)
            nameLabel.text = "Name: " + nameBuffer + "_";
    }

    void FinishNameEntry()
    {
        enteringName = false;
        if (Keyboard.current != null)
            Keyboard.current.onTextInput -= OnTextInput;

        if (LeaderboardManager.Instance != null)
            LeaderboardManager.Instance.PlayerName = nameBuffer;

        UpdateLabel();
    }

    void UpdateLabel()
    {
        if (nameLabel == null) return;
        string name = LeaderboardManager.Instance != null ? LeaderboardManager.Instance.PlayerName : "Player";
        nameLabel.text = "Name: " + name;
    }
}
