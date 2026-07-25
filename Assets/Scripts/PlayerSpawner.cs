using TMPro;
using UnityEngine;
using static StartScreenUI;

public class PlayerSpawner : MonoBehaviour
{
    public Transform playerSpawnPoint;
    public TMP_Text healthTextRef;

    public void SpawnPlayer()
    {
        if (GameSelection.SelectedCharacterPrefab == null)
        {
            Debug.LogError("[PlayerSpawner] No character prefab selected!");
            return;
        }

        GameObject player = Instantiate(
            GameSelection.SelectedCharacterPrefab,
            playerSpawnPoint.position,
            playerSpawnPoint.rotation
        );
        player.tag = "Player";

        var playerScript = player.GetComponent<Player>();
        if (playerScript != null)
        {
            playerScript.healthText = healthTextRef;
            playerScript.enabled = true;
        }

        // Weapon spawning is already handled by Player.Start() -> EquipSelectedWeapon()
    }
}