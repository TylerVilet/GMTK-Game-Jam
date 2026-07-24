using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Drops a barrage of small asteroids into the arena within the first 13s of
// every wave, in groups of 1-3 falling at once. Player and enemies both move
// via dynamic Rigidbody2D (Player.rb.linearVelocity, EnemyChase.rb.MovePosition),
// so a plain static BoxCollider2D is enough to make them physically slide
// around each asteroid - no pathfinding needed.
public class BlockadeManager : MonoBehaviour
{
    const int MaxAsteroids = 30;
    const float AsteroidWidth = 1.5f;
    const float AsteroidHeight = 1.5f;
    const float CornerMargin = 6f;       // keep clear of the four corners
    const float EnemyFootprint = 1f;     // matches Enemy.prefab's collider size
    const float MinGapBetween = EnemyFootprint * 2f; // 2 enemy-lengths of clearance between asteroids
    const float MinDistanceBetween = AsteroidWidth + MinGapBetween;
    const int MaxPlacementAttempts = 30;
    const float DropHeight = 6f;
    const float DropDuration = 0.35f;

    const float BarrageWindow = 13f;     // the whole barrage happens within this many seconds of the wave starting
    const int BarrageMinCount = 6;       // wave 0 drops 6-10 asteroids total, +1 more each wave after
    const int BarrageMaxCount = 10;
    const int MinSimultaneous = 1;       // 1-3 asteroids can fall together in a single drop
    const int MaxSimultaneous = 3;

    const float WarningDuration = 1.2f;  // telegraph shown before it actually lands
    const float WarningPulseSpeed = 10f;
    const float LandingDamage = 20f;
    const float KnockbackDistance = 3.5f; // clear of the footprint if caught underneath

    readonly List<GameObject> activeAsteroids = new List<GameObject>();
    WaveManager subscribedWaveManager;
    Coroutine barrageRoutine;
    Sprite asteroidSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        GameObject root = new GameObject("BlockadeManager");
        DontDestroyOnLoad(root);
        root.AddComponent<BlockadeManager>();
    }

    void Awake()
    {
        asteroidSprite = CreateRoundSprite();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (subscribedWaveManager != null)
        {
            subscribedWaveManager.OnWaveStarted.RemoveListener(OnWaveStarted);
            subscribedWaveManager.OnWaveCleared.RemoveListener(ClearAsteroids);
        }
    }

    void Start()
    {
        SubscribeToWaveManager();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StopAllCoroutines(); // cancel any pending warning/drop from the scene that just unloaded
        barrageRoutine = null;
        activeAsteroids.Clear(); // the previous scene (and its asteroids) is already gone
        SubscribeToWaveManager();
    }

    void SubscribeToWaveManager()
    {
        if (subscribedWaveManager != null)
        {
            subscribedWaveManager.OnWaveStarted.RemoveListener(OnWaveStarted);
            subscribedWaveManager.OnWaveCleared.RemoveListener(ClearAsteroids);
        }

        subscribedWaveManager = WaveManager.Instance;

        if (subscribedWaveManager != null)
        {
            subscribedWaveManager.OnWaveStarted.AddListener(OnWaveStarted);
            subscribedWaveManager.OnWaveCleared.AddListener(ClearAsteroids);
        }
    }

    void OnWaveStarted(int waveIndex)
    {
        if (barrageRoutine != null) StopCoroutine(barrageRoutine);
        ClearAsteroids();
        barrageRoutine = StartCoroutine(RunBarrage(waveIndex));
    }

    // The asteroids from this wave are done their job once the wave clears -
    // whatever's still standing gets cleared along with it.
    void ClearAsteroids()
    {
        foreach (GameObject asteroid in activeAsteroids)
            if (asteroid != null) Destroy(asteroid);
        activeAsteroids.Clear();
    }

    // Splits the wave's total asteroid count into groups of 1-3 falling
    // together, and spreads those groups across the first 13s of the wave.
    IEnumerator RunBarrage(int waveIndex)
    {
        WaveManager wm = subscribedWaveManager;
        if (wm == null) yield break;

        int totalCount = Random.Range(BarrageMinCount, BarrageMaxCount + 1) + waveIndex;

        List<int> groups = new List<int>();
        int remaining = totalCount;
        while (remaining > 0)
        {
            int size = Mathf.Min(remaining, Random.Range(MinSimultaneous, MaxSimultaneous + 1));
            groups.Add(size);
            remaining -= size;
        }

        List<float> startTimes = new List<float>();
        for (int i = 0; i < groups.Count; i++)
            startTimes.Add(Random.Range(0f, BarrageWindow));
        startTimes.Sort();

        float elapsed = 0f;
        for (int i = 0; i < groups.Count; i++)
        {
            float wait = Mathf.Max(0f, startTimes[i] - elapsed);
            yield return new WaitForSeconds(wait);
            elapsed += wait;

            if (wm == null || wm.IsGameOver) yield break;

            ArenaBounds bounds = FindFirstObjectByType<ArenaBounds>();
            if (bounds == null) continue;

            for (int j = 0; j < groups[i]; j++)
            {
                Vector2 position = FindPosition(bounds);
                StartCoroutine(WarnAndDrop(position));
            }
        }
    }

    IEnumerator WarnAndDrop(Vector2 position)
    {
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

        if (subscribedWaveManager == null || subscribedWaveManager.IsGameOver) yield break;
        SpawnAsteroid(position);
    }

    GameObject CreateWarningMarker(Vector2 position)
    {
        GameObject warning = new GameObject("AsteroidWarning");
        warning.transform.position = new Vector3(position.x, position.y, 0f);
        warning.transform.localScale = new Vector3(AsteroidWidth, AsteroidHeight, 1f);

        SpriteRenderer renderer = warning.AddComponent<SpriteRenderer>();
        renderer.sprite = asteroidSprite;
        renderer.color = new Color(1f, 0.15f, 0.1f, 0.35f);
        renderer.sortingOrder = 1;

        return warning;
    }

    void SpawnAsteroid(Vector2 position)
    {
        if (activeAsteroids.Count >= MaxAsteroids)
        {
            GameObject oldest = activeAsteroids[0];
            activeAsteroids.RemoveAt(0);
            if (oldest != null) Destroy(oldest);
        }

        GameObject asteroid = new GameObject("Asteroid");
        asteroid.transform.localScale = new Vector3(AsteroidWidth, AsteroidHeight, 1f);

        SpriteRenderer renderer = asteroid.AddComponent<SpriteRenderer>();
        renderer.sprite = asteroidSprite;
        renderer.color = new Color(0.42f, 0.39f, 0.36f);

        BoxCollider2D collider = asteroid.AddComponent<BoxCollider2D>();
        collider.enabled = false; // turned on once it lands, so it can't clip anything mid-drop

        activeAsteroids.Add(asteroid);
        StartCoroutine(DropIn(asteroid, position));
    }

    // Rejection-samples a spot anywhere in the arena away from the corners and
    // other asteroids - the player's position doesn't factor in, so a barrage
    // can land anywhere, including right where the player's standing. Falls
    // back to the least-bad candidate seen if nothing fully qualifies.
    Vector2 FindPosition(ArenaBounds bounds)
    {
        float halfW = bounds.halfWidth - AsteroidWidth * 0.5f - 1f;
        float halfH = bounds.halfHeight - AsteroidHeight * 0.5f - 1f;

        Vector2 best = Vector2.zero;
        float bestMargin = float.MinValue;

        for (int attempt = 0; attempt < MaxPlacementAttempts; attempt++)
        {
            Vector2 candidate = new Vector2(Random.Range(-halfW, halfW), Random.Range(-halfH, halfH));

            bool nearVerticalEdge = Mathf.Abs(candidate.x) > halfW - CornerMargin;
            bool nearHorizontalEdge = Mathf.Abs(candidate.y) > halfH - CornerMargin;
            if (nearVerticalEdge && nearHorizontalEdge) continue; // sitting in a corner

            float margin = float.MaxValue;
            foreach (GameObject existing in activeAsteroids)
            {
                if (existing == null) continue;
                margin = Mathf.Min(margin, Vector2.Distance(candidate, existing.transform.position) - MinDistanceBetween);
            }

            if (margin >= 0f) return candidate;

            if (margin > bestMargin)
            {
                bestMargin = margin;
                best = candidate;
            }
        }

        return best;
    }

    IEnumerator DropIn(GameObject asteroid, Vector2 restPosition)
    {
        Vector3 restPos3 = new Vector3(restPosition.x, restPosition.y, 0f);
        Vector3 startPos = restPos3 + new Vector3(0f, DropHeight, 0f);

        Transform t = asteroid.transform;
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

        BoxCollider2D collider = asteroid.GetComponent<BoxCollider2D>();
        if (collider != null) collider.enabled = true;

        ApplyLandingImpact(restPos3);
    }

    // Anything still standing in the footprint when it lands takes damage and
    // gets knocked clear of it, so nothing ends up stuck underneath.
    void ApplyLandingImpact(Vector3 center)
    {
        Vector2 size = new Vector2(AsteroidWidth, AsteroidHeight);
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

    static Sprite CreateRoundSprite()
    {
        const int size = 32;
        Texture2D texture = new Texture2D(size, size);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                texture.SetPixel(x, y, dist <= radius ? Color.white : Color.clear);
            }
        }
        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
