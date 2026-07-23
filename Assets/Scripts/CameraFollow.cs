using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public float orthographicSize = 10f;
    public float smoothTime = 0.15f;

    private Camera cam;
    private Transform player;
    private Vector3 velocity;
    private ArenaBounds arena;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.GetComponent<CameraFollow>() == null)
        {
            mainCamera.gameObject.AddComponent<CameraFollow>();
        }
    }

    void Start()
    {
        cam = GetComponent<Camera>();

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        Player playerComponent = playerObject != null ? playerObject.GetComponent<Player>() : FindFirstObjectByType<Player>();
        player = playerComponent?.transform;

        arena = FindFirstObjectByType<ArenaBounds>();
    }

    void LateUpdate()
    {
        if (player == null)
        {
            return;
        }

        if (cam != null)
        {
            cam.orthographicSize = orthographicSize;
        }

        Vector3 target = new Vector3(player.position.x, player.position.y, transform.position.z);
        Vector3 smoothed = Vector3.SmoothDamp(transform.position, target, ref velocity, smoothTime);
        transform.position = ClampToArena(smoothed);
    }

    Vector3 ClampToArena(Vector3 position)
    {
        if (arena == null || cam == null)
        {
            return position;
        }

        // Clamp to the outer face of the border walls so the black border itself
        // stays visible but nothing beyond it ever comes into view.
        float arenaHalfWidth = arena.halfWidth + arena.wallThickness;
        float arenaHalfHeight = arena.halfHeight + arena.wallThickness;

        float camHalfHeight = cam.orthographicSize;
        float camHalfWidth = camHalfHeight * cam.aspect;

        float x = camHalfWidth >= arenaHalfWidth
            ? 0f
            : Mathf.Clamp(position.x, -arenaHalfWidth + camHalfWidth, arenaHalfWidth - camHalfWidth);
        float y = camHalfHeight >= arenaHalfHeight
            ? 0f
            : Mathf.Clamp(position.y, -arenaHalfHeight + camHalfHeight, arenaHalfHeight - camHalfHeight);

        return new Vector3(x, y, position.z);
    }
}
