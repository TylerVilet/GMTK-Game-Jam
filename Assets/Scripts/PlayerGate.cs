using UnityEngine;

// Put this on the Player object. Disables movement/shooting until WaveManager
// says the game has begun (i.e. the Start button was pressed on the UI scene).
public class PlayerGate : MonoBehaviour
{
    Player playerMovement;
    Gun gun;

    void Awake()
    {
        playerMovement = GetComponent<Player>();
        gun = GetComponentInChildren<Gun>();

        if (playerMovement != null) playerMovement.enabled = false;
        if (gun != null) gun.enabled = false;
    }

    void Start()
    {
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnGameBegin.AddListener(EnablePlayer);
    }

    void OnDestroy()
    {
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnGameBegin.RemoveListener(EnablePlayer);
    }

    void EnablePlayer()
    {
        if (playerMovement != null) playerMovement.enabled = true;
        if (gun != null) gun.enabled = true;
    }
}
