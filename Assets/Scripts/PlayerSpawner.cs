using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StartScreenUI;

public class PlayerSpawner : MonoBehaviour
{
    public Transform playerSpawnPoint;
    public TMP_Text healthTextRef;
    public Image healthBarFillRef;

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
            playerScript.healthBarFill = healthBarFillRef;
            playerScript.enabled = true;
        }

        // NEW: tell the camera who to follow
        CameraFollow cameraFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (cameraFollow != null)
            cameraFollow.SetPlayer(player.transform);
    }
}