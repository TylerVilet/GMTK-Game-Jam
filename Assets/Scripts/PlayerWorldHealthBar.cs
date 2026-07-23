using UnityEngine;

public class PlayerWorldHealthBar : MonoBehaviour
{
    public float width = 1f;
    public float height = 0.12f;
    public Vector3 offset = new Vector3(0f, 0.7f, 0f);

    private Player player;
    private Transform fill;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        new GameObject("PlayerWorldHealthBar").AddComponent<PlayerWorldHealthBar>();
    }

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        player = playerObject != null ? playerObject.GetComponent<Player>() : null;
        if (player == null)
        {
            player = FindFirstObjectByType<Player>();
        }

        if (player == null)
        {
            Destroy(gameObject);
            return;
        }

        BuildBar();
    }

    void BuildBar()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        // Pivot on the left edge so shrinking the scale keeps the left edge fixed
        // and only moves the right edge inward, i.e. the bar drains right to left.
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0f, 0.5f), 1f);

        Vector3 playerScale = player.transform.localScale;
        GameObject anchor = new GameObject("HealthBarAnchor");
        anchor.transform.SetParent(player.transform, false);
        anchor.transform.localPosition = offset;
        anchor.transform.localScale = new Vector3(
            playerScale.x != 0f ? 1f / playerScale.x : 1f,
            playerScale.y != 0f ? 1f / playerScale.y : 1f,
            1f);

        GameObject fillObject = new GameObject("HealthBarFill");
        fillObject.transform.SetParent(anchor.transform, false);
        fillObject.transform.localPosition = new Vector3(-width * 0.5f, 0f, 0f);
        fillObject.transform.localScale = new Vector3(width, height, 1f);

        SpriteRenderer renderer = fillObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = Color.red;
        renderer.sortingOrder = 10;

        fill = fillObject.transform;
    }

    void Update()
    {
        if (player == null || fill == null)
        {
            return;
        }

        float percent = player.maxHealth > 0f ? Mathf.Clamp01(player.health / player.maxHealth) : 0f;
        fill.localScale = new Vector3(width * percent, height, 1f);
    }
}
