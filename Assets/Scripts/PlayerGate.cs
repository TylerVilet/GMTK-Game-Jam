using UnityEngine;

// Put this on the Player object. Disables movement/shooting until WaveManager
// says the game has begun (i.e. the Start button was pressed on the UI scene).
public class PlayerGate : MonoBehaviour
{
    Movement movement;
    Gun gun;

    void Awake()
    {
        movement = GetComponent<Movement>();
        gun = GetComponentInChildren<Gun>();

        if (movement != null) movement.enabled = false;
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
        if (movement != null) movement.enabled = true;
        if (gun != null) gun.enabled = true;
    }
}
