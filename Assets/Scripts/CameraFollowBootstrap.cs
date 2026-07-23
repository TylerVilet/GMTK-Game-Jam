using UnityEngine;
using UnityEngine.SceneManagement;

// Camera.main itself is destroyed and recreated every time GameScene reloads
// (e.g. on restart), so CameraFollow can't persist on the camera directly.
// This persists instead and re-attaches CameraFollow to whichever camera
// exists after each scene load.
public class CameraFollowBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        GameObject root = new GameObject("CameraFollowBootstrap");
        DontDestroyOnLoad(root);
        root.AddComponent<CameraFollowBootstrap>();
    }

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        AttachToCamera();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AttachToCamera();
    }

    void AttachToCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.GetComponent<CameraFollow>() == null)
        {
            mainCamera.gameObject.AddComponent<CameraFollow>();
        }
    }
}
