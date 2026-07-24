using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Drops a rectangular blockade into the arena at the start of every wave.
// Player and enemies both move via dynamic Rigidbody2D (Player.rb.linearVelocity,
// EnemyChase.rb.MovePosition), so a plain static BoxCollider2D is enough to make
// them physically slide around it - no pathfinding needed.
public class BlockadeManager : MonoBehaviour
{
    const int MaxBlockades = 8;
    const float BlockadeWidth = 3.5f;
    const float BlockadeHeight = 3.5f;
    const float CornerMargin = 6f;       // keep clear of the four corners
    const float MinDistanceBetween = 6f; // never let a new one land on/next to an old one
    const float MinDistanceFromPlayer = 6f;
    const int MaxPlacementAttempts = 30;
    const float DropHeight = 6f;
    const float DropDuration = 0.35f;

    const float EarliestDropDelay = 5f;   // never before 5s into the wave
    const float LatestDropMargin = 30f;   // never within the last 30s of the wave's time limit
    const float WarningDuration = 1.2f;   // telegraph shown before it actually lands
    const float WarningPulseSpeed = 10f;
    const float LandingDamage = 10f;
    const float KnockbackDistance = 3.5f; // clear of the footprint if caught underneath

    readonly List<GameObject> activeBlockades = new List<GameObject>();
    WaveManager subscribedWaveManager;
    Coroutine scheduledDrop;
    Sprite blockadeSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        GameObject root = new GameObject("BlockadeManager");
        DontDestroyOnLoad(root);
        root.AddComponent<BlockadeManager>();
    }

    void Awake()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        blockadeSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (subscribedWaveManager != null)
            subscribedWaveManager.OnWaveStarted.RemoveListener(OnWaveStarted);
    }

    void Start()
    {
        SubscribeToWaveManager();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StopAllCoroutines(); // cancel any pending warning/drop from the scene that just unloaded
        scheduledDrop = null;
        activeBlockades.Clear(); // the previous scene (and its blockades) is already gone
        SubscribeToWaveManager();
    }

    void SubscribeToWaveManager()
    {
        if (subscribedWaveManager != null)
            subscribedWaveManager.OnWaveStarted.RemoveListener(OnWaveStarted);

        subscribedWaveManager = WaveManager.Instance;

        if (subscribedWaveManager != null)
            subscribedWaveManager.OnWaveStarted.AddListener(OnWaveStarted);
    }

    void OnWaveStarted(int waveIndex)
    {
        if (scheduledDrop != null) StopCoroutine(scheduledDrop);
        scheduledDrop = StartCoroutine(ScheduleDrop());
    }

    // Waits a random amount of time within [5s, timeLimit - 30s] of the wave's
    // clock, then shows a warning telegraph before actually dropping the blockade.
    IEnumerator ScheduleDrop()
    {
        WaveManager wm = subscribedWaveManager;
        if (wm == null) yield break;

        float latestDelay = Mathf.Max(EarliestDropDelay, wm.timeLimit - LatestDropMargin);
        float delay = Random.Range(EarliestDropDelay, latestDelay);
        yield return new WaitForSeconds(delay);

        if (wm == null || wm.IsGameOver) yield break;

        ArenaBounds bounds = FindFirstObjectByType<ArenaBounds>();
        if (bounds == null) yield break;

        Vector2 position = FindPosition(bounds);

        GameObject warning = CreateWarningMarker(position);
        float elapsed = 0f;
        while (elapsed < WarningDuration)
        {
            if (warning == null) yield break;
            elapsed += Time.deltaTime;
            SpriteRenderer sr = warning.GetComponent<SpriteRenderer>();
            Color c = sr.color;
            c.a = 0.35f + 0.35f * Mathf.Sin(elapsed * WarningPulseSpeed);
            sr.color = c;
            yield return null;
        }
        if (warning != null) Destroy(warning);

        if (wm == null || wm.IsGameOver) yield break;
        SpawnBlockade(position);
    }

    GameObject CreateWarningMarker(Vector2 position)
    {
        GameObject warning = new GameObject("BlockadeWarning");
        warning.transform.position = new Vector3(position.x, position.y, 0f);
        warning.transform.localScale = new Vector3(BlockadeWidth, BlockadeHeight, 1f);

        SpriteRenderer renderer = warning.AddComponent<SpriteRenderer>();
        renderer.sprite = blockadeSprite;
        renderer.color = new Color(1f, 0.15f, 0.1f, 0.35f);
        renderer.sortingOrder = 1;

        return warning;
    }

    void SpawnBlockade(Vector2 position)
    {
        if (activeBlockades.Count >= MaxBlockades)
        {
            GameObject oldest = activeBlockades[0];
            activeBlockades.RemoveAt(0);
            if (oldest != null) Destroy(oldest);
        }

        GameObject blockade = new GameObject("Blockade");
        blockade.transform.localScale = new Vector3(BlockadeWidth, BlockadeHeight, 1f);

        SpriteRenderer renderer = blockade.AddComponent<SpriteRenderer>();
        renderer.sprite = blockadeSprite;
        renderer.color = new Color(0.5f, 0.36f, 0.22f);

        BoxCollider2D collider = blockade.AddComponent<BoxCollider2D>();
        collider.enabled = false; // turned on once it lands, so it can't clip anything mid-drop

        activeBlockades.Add(blockade);
        StartCoroutine(DropIn(blockade, position));
    }

    // Rejection-samples a spot away from the corners, other blockades, and the
    // player. Falls back to the least-bad candidate seen if nothing fully qualifies.
    Vector2 FindPosition(ArenaBounds bounds)
    {
        float halfW = bounds.halfWidth - BlockadeWidth * 0.5f - 1f;
        float halfH = bounds.halfHeight - BlockadeHeight * 0.5f - 1f;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        Vector2? playerPos = playerObject != null ? (Vector2?)playerObject.transform.position : null;

        Vector2 best = Vector2.zero;
        float bestMargin = float.MinValue;

        for (int attempt = 0; attempt < MaxPlacementAttempts; attempt++)
        {
            Vector2 candidate = new Vector2(Random.Range(-halfW, halfW), Random.Range(-halfH, halfH));

            bool nearVerticalEdge = Mathf.Abs(candidate.x) > halfW - CornerMargin;
            bool nearHorizontalEdge = Mathf.Abs(candidate.y) > halfH - CornerMargin;
            if (nearVerticalEdge && nearHorizontalEdge) continue; // sitting in a corner

            float margin = float.MaxValue;
            foreach (GameObject existing in activeBlockades)
            {
                if (existing == null) continue;
                margin = Mathf.Min(margin, Vector2.Distance(candidate, existing.transform.position) - MinDistanceBetween);
            }
            if (playerPos.HasValue)
                margin = Mathf.Min(margin, Vector2.Distance(candidate, playerPos.Value) - MinDistanceFromPlayer);

            if (margin >= 0f) return candidate;

            if (margin > bestMargin)
            {
                bestMargin = margin;
                best = candidate;
            }
        }

        return best;
    }

    IEnumerator DropIn(GameObject blockade, Vector2 restPosition)
    {
        Vector3 restPos3 = new Vector3(restPosition.x, restPosition.y, 0f);
        Vector3 startPos = restPos3 + new Vector3(0f, DropHeight, 0f);

        Transform t = blockade.transform;
        t.position = startPos;

        float elapsed = 0f;
        while (elapsed < DropDuration)
        {
            if (t == null) yield break; // scene reloaded mid-drop
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / DropDuration);
            t.position = Vector3.Lerp(startPos, restPos3, progress * progress); // ease-in like it's actually falling
            yield return null;
        }

        if (t == null) yield break;
        t.position = restPos3;

        BoxCollider2D collider = blockade.GetComponent<BoxCollider2D>();
        if (collider != null) collider.enabled = true;

        ApplyLandingImpact(restPos3);
    }

    // Anything still standing in the footprint when it lands takes damage and
    // gets knocked clear of it, so nothing ends up stuck underneath.
    void ApplyLandingImpact(Vector3 center)
    {
        Vector2 size = new Vector2(BlockadeWidth, BlockadeHeight);
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f);

        foreach (Collider2D hit in hits)
        {
            bool isPlayer = hit.CompareTag("Player");
            bool isEnemy = hit.CompareTag("Enemy");
            if (!isPlayer && !isEnemy) continue;

            Vector2 pushDir = (Vector2)hit.transform.position - (Vector2)center;
            pushDir = pushDir.sqrMagnitude > 0.0001f ? pushDir.normalized : Random.insideUnitCircle.normalized;

            Rigidbody2D rb = hit.attachedRigidbody;
            if (rb != null) rb.position += pushDir * KnockbackDistance;

            if (isPlayer)
            {
                Player player = hit.GetComponent<Player>();
                if (player != null) player.loseHealth(LandingDamage);
            }
            else
            {
                Enemy enemy = hit.GetComponent<Enemy>();
                if (enemy != null) enemy.TakeDamage(LandingDamage);
            }
        }
    }
}
