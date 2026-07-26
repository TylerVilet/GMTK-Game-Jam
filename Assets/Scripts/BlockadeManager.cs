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
    static readonly float[] AsteroidSizes = { 1f, 1.5f }; // small / medium - picked per asteroid
    const float CornerMargin = 6f;       // keep clear of the four corners
    const float EnemyFootprint = 1f;     // matches Enemy.prefab's collider size
    const float EdgeClearance = EnemyFootprint * 3f; // guaranteed gap between two asteroids' edges, regardless of their sizes
    const int MaxPlacementAttempts = 50;
    const float DropHeight = 6f;
    const float DropDuration = 0.35f;

    const float BarrageWindow = 13f;     // the whole barrage happens within this many seconds of the wave starting
    const int BarrageMinCount = 4;       // wave 0 drops 4-7 asteroids total, +1 more each wave after
    const int BarrageMaxCount = 7;
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
        // Loaded from Resources rather than wired in the Inspector - this
        // component is created entirely from code (see Bootstrap below), so
        // there's no scene object to hang a serialized field reference off of.
        asteroidSprite = Resources.Load<Sprite>("asteroid");
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
                float size = AsteroidSizes[Random.Range(0, AsteroidSizes.Length)];
                Vector2 position = FindPosition(bounds, size);
                StartCoroutine(WarnAndDrop(position, size));
            }
        }
    }

    IEnumerator WarnAndDrop(Vector2 position, float size)
    {
        GameObject warning = CreateWarningMarker(position, size);
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
        SpawnAsteroid(position, size);
    }

    GameObject CreateWarningMarker(Vector2 position, float size)
    {
        GameObject warning = new GameObject("AsteroidWarning");
        warning.transform.position = new Vector3(position.x, position.y, 0f);
        warning.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        warning.transform.localScale = new Vector3(size, size, 1f);

        SpriteRenderer renderer = warning.AddComponent<SpriteRenderer>();
        renderer.sprite = asteroidSprite;
        renderer.color = new Color(1f, 0.15f, 0.1f, 0.35f); // red tint over the real art, not its true color
        renderer.sortingOrder = 1;

        return warning;
    }

    void SpawnAsteroid(Vector2 position, float size)
    {
        if (activeAsteroids.Count >= MaxAsteroids)
        {
            GameObject oldest = activeAsteroids[0];
            activeAsteroids.RemoveAt(0);
            if (oldest != null) Destroy(oldest);
        }

        GameObject asteroid = new GameObject("Asteroid");
        asteroid.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)); // so a run of asteroids never reads as identical stamped copies
        asteroid.transform.localScale = new Vector3(size, size, 1f);

        SpriteRenderer renderer = asteroid.AddComponent<SpriteRenderer>();
        renderer.sprite = asteroidSprite;
        renderer.color = Color.white; // show the real art's own colors, no tint

        BoxCollider2D collider = asteroid.AddComponent<BoxCollider2D>();
        collider.enabled = false; // turned on once it lands, so it can't clip anything mid-drop

        activeAsteroids.Add(asteroid);
        StartCoroutine(DropIn(asteroid, position));
    }

    // Finds a spot anywhere in the arena away from other asteroids - the
    // player's position doesn't factor in, so a barrage can land anywhere,
    // including right where the player's standing.
    //
    // The candidate range goes all the way out to the wall line rather than
    // stopping a full asteroid-width short of it, so asteroids can land
    // overlapping the edge - anywhere from barely clipping it to about half
    // hanging out past the boundary - instead of always sitting fully inside.
    // That also opens up a lot more usable placement area near the edges,
    // which helps the spacing rule below actually find room in a crowded arena.
    //
    // Tries progressively more lenient passes rather than a single one with a
    // generic "best effort" fallback, so a crowded arena relaxes the corner
    // rule (harmless) long before it ever relaxes asteroid-to-asteroid
    // spacing (which is what caused them to visibly touch/overlap).
    Vector2 FindPosition(ArenaBounds bounds, float size)
    {
        float halfW = bounds.halfWidth;
        float halfH = bounds.halfHeight;

        Vector2? pos = TrySample(halfW, halfH, respectCorners: true, size, edgeClearance: EdgeClearance);
        if (pos.HasValue) return pos.Value;

        pos = TrySample(halfW, halfH, respectCorners: false, size, edgeClearance: EdgeClearance);
        if (pos.HasValue) return pos.Value;

        // Last resort: still never allow full overlap, just accept tighter
        // spacing than the "enemy can squeeze through" rule normally wants.
        pos = TrySample(halfW, halfH, respectCorners: false, size, edgeClearance: 0f);
        if (pos.HasValue) return pos.Value;

        return new Vector2(Random.Range(-halfW, halfW), Random.Range(-halfH, halfH));
    }

    // minDistance is computed per existing asteroid from both its actual size
    // and the new one's, so the required gap always matches whatever sizes
    // are actually involved instead of assuming a single fixed asteroid size.
    Vector2? TrySample(float halfW, float halfH, bool respectCorners, float size, float edgeClearance)
    {
        for (int attempt = 0; attempt < MaxPlacementAttempts; attempt++)
        {
            Vector2 candidate = new Vector2(Random.Range(-halfW, halfW), Random.Range(-halfH, halfH));

            if (respectCorners)
            {
                bool nearVerticalEdge = Mathf.Abs(candidate.x) > halfW - CornerMargin;
                bool nearHorizontalEdge = Mathf.Abs(candidate.y) > halfH - CornerMargin;
                if (nearVerticalEdge && nearHorizontalEdge) continue; // sitting in a corner
            }

            bool clear = true;
            foreach (GameObject existing in activeAsteroids)
            {
                if (existing == null) continue;
                float existingSize = existing.transform.localScale.x;
                float minDistance = (size + existingSize) * 0.5f + edgeClearance;
                if (Vector2.Distance(candidate, existing.transform.position) < minDistance)
                {
                    clear = false;
                    break;
                }
            }

            if (clear) return candidate;
        }

        return null;
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

        ApplyLandingImpact(restPos3, t.localScale.x);
    }

    // Anything still standing in the footprint when it lands takes damage and
    // gets knocked clear of it, so nothing ends up stuck underneath.
    void ApplyLandingImpact(Vector3 center, float asteroidSize)
    {
        Vector2 size = new Vector2(asteroidSize, asteroidSize);
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
