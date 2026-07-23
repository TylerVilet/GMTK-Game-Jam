using UnityEngine;

public class ArenaBounds : MonoBehaviour
{
    public float halfWidth = 18f;
    public float halfHeight = 11f;
    public float wallThickness = 2f;

    private Sprite wallSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        new GameObject("ArenaBounds").AddComponent<ArenaBounds>();
    }

    void Awake()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        wallSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);

        CreateWall("WallTop", new Vector2(0f, halfHeight + wallThickness * 0.5f), new Vector2(halfWidth * 2f + wallThickness * 2f, wallThickness));
        CreateWall("WallBottom", new Vector2(0f, -halfHeight - wallThickness * 0.5f), new Vector2(halfWidth * 2f + wallThickness * 2f, wallThickness));
        CreateWall("WallLeft", new Vector2(-halfWidth - wallThickness * 0.5f, 0f), new Vector2(wallThickness, halfHeight * 2f + wallThickness * 2f));
        CreateWall("WallRight", new Vector2(halfWidth + wallThickness * 0.5f, 0f), new Vector2(wallThickness, halfHeight * 2f + wallThickness * 2f));
    }

    void CreateWall(string wallName, Vector2 position, Vector2 size)
    {
        GameObject wall = new GameObject(wallName);
        wall.transform.SetParent(transform, false);
        wall.transform.position = position;
        // Default BoxCollider2D size is (1,1); scaling the transform grows both
        // the collider and the sprite to `size` together without double-scaling.
        wall.transform.localScale = new Vector3(size.x, size.y, 1f);

        wall.AddComponent<BoxCollider2D>();

        SpriteRenderer renderer = wall.AddComponent<SpriteRenderer>();
        renderer.sprite = wallSprite;
        renderer.color = Color.black;
        renderer.sortingOrder = -1;
    }
}
