using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using static StartScreenUI;

// UnityEvent<T> needs a concrete subclass to show its argument fields in the Inspector.
[System.Serializable] public class IntEvent : UnityEvent<int> { }
[System.Serializable] public class IntIntEvent : UnityEvent<int, int> { }
[System.Serializable] public class FloatEvent : UnityEvent<float> { }

// One alien type - only starts spawning once the wave count reaches minWave.
[System.Serializable]
public class EnemyType
{
    public GameObject prefab;
    public int minWave = 0; // wave index (0-based) this type is allowed to start appearing in
}

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Waves (unlimited - keeps going until you die)")]
    public EnemyType[] enemyTypes;
    public Transform[] spawnPoints;
    public float timeLimit = 60f;
    public int startingEnemyCount = 50;
    public int enemyCountIncreasePerWave = 10;
    public string enemyTag = "Enemy";
    public float spawnInterval = 5f;
    public float stopwatch;
    public float waveClearedPopupDelay = 1.5f; // pause between the wave clearing and the power-up popup appearing

    [Header("UI Scene (loaded additively alongside this one)")]
    public string uiSceneName = "MainMenu";

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI waveText;

    [Header("Events (hook UI / other systems to these)")]
    public UnityEvent OnGameBegin;           // Start button was pressed - unlock the player
    public IntEvent OnWaveStarted;           // wave index
    public IntIntEvent OnEnemiesLeftChanged; // (remaining, total)
    public FloatEvent OnCountdownChanged;    // seconds remaining
    public UnityEvent OnWaveCleared;         // show the upgrade popup here, then call StartNextWave() when the player hits Start
    public UnityEvent OnGameOver;

    public int CurrentWaveIndex { get; private set; } = -1;
    public int EnemiesRemaining { get; private set; }
    public float Countdown { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool IsWaitingForNextWave { get; private set; }

    bool waveActive;
    List<GameObject> eligiblePrefabs = new List<GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (!string.IsNullOrEmpty(uiSceneName) && !SceneManager.GetSceneByName(uiSceneName).isLoaded)
            SceneManager.LoadScene(uiSceneName, LoadSceneMode.Additive);
    }

    // Call this from the start screen's Start button once the player has picked a character/weapon.
    public void BeginGame()
    {
        if (IsGameOver) return;


        OnGameBegin?.Invoke();
        StartCoroutine(RunWaves());

        if (!string.IsNullOrEmpty(uiSceneName))
            SceneManager.UnloadSceneAsync(uiSceneName);

        if (MusicManager.Instance != null)
            MusicManager.Instance.Play();
    }


    void Update()
    {
        if (!waveActive || IsGameOver) return;

        Countdown -= Time.deltaTime;
        OnCountdownChanged?.Invoke(Mathf.Max(Countdown, 0f));

        if (Countdown <= 0f)
        {
            GameOver();
        }
    }

    IEnumerator RunWaves()
    {
        while (!IsGameOver)
        {
            CurrentWaveIndex++;
            yield return StartCoroutine(RunWave());

            if (IsGameOver) yield break;

            Debug.Log($"[WaveManager] Wave {CurrentWaveIndex} cleared.");

            // add remaining time at end of wave to score
            int timeBonus = Mathf.RoundToInt(Mathf.Max(0f, Countdown));
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.AddScore(timeBonus * (1 + CurrentWaveIndex));

            yield return new WaitForSeconds(waveClearedPopupDelay);
            if (IsGameOver) yield break;

            IsWaitingForNextWave = true;
            OnWaveCleared?.Invoke();

            yield return new WaitUntil(() => !IsWaitingForNextWave || IsGameOver);

            if (IsGameOver) yield break;
        }
    }

    IEnumerator RunWave()
    {

        int enemyCount = startingEnemyCount + CurrentWaveIndex * enemyCountIncreasePerWave;
        BuildEligiblePrefabs();

        OnWaveStarted?.Invoke(CurrentWaveIndex);
        UpdateWaveText();
        Debug.Log($"[WaveManager] Wave {CurrentWaveIndex} started: {enemyCount} enemies, {timeLimit}s to clear.");

        EnemiesRemaining = enemyCount;
        Countdown = timeLimit;
        waveActive = true;
        OnEnemiesLeftChanged?.Invoke(EnemiesRemaining, enemyCount);

        for (int i = 0; i < enemyCount; i++)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(spawnInterval / enemyCount); // spread spawns across a few frames instead of all at once
        }

        while (EnemiesRemaining > 0 && !IsGameOver)
        {
            yield return null;
        }

        waveActive = false;
    }

    void UpdateWaveText()
    {
        if (waveText != null)
            waveText.text = $"Wave {CurrentWaveIndex + 1}"; // +1 so it displays as 1-based to the player
    }

    void BuildEligiblePrefabs()
    {
        eligiblePrefabs.Clear();
        if (enemyTypes == null) return;

        foreach (var type in enemyTypes)
        {
            if (type.prefab != null && CurrentWaveIndex >= type.minWave)
                eligiblePrefabs.Add(type.prefab);
        }
    }

    [Header("Spawn Safety")]
    public float minSpawnDistanceFromPlayer = 5f;

    void SpawnEnemy()
    {
        if (eligiblePrefabs.Count == 0) return;
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        Transform point = GetSafeSpawnPoint();
        if (point == null) return;

        GameObject prefab = eligiblePrefabs[Random.Range(0, eligiblePrefabs.Count)];
        GameObject enemy = Instantiate(prefab, point.position, point.rotation);

        if (!enemy.CompareTag(enemyTag))
            enemy.tag = enemyTag;
    }

    Transform GetSafeSpawnPoint()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        Vector3 playerPos = playerObject != null ? playerObject.transform.position : Vector3.positiveInfinity;

        // Points far enough from the player - pick randomly among ALL of these,
        // not just the single farthest one.
        List<Transform> safePoints = new List<Transform>();
        foreach (var point in spawnPoints)
        {
            if (Vector3.Distance(point.position, playerPos) >= minSpawnDistanceFromPlayer)
                safePoints.Add(point);
        }

        if (safePoints.Count > 0)
            return safePoints[Random.Range(0, safePoints.Count)];

        // Fallback: no point clears the safe distance (e.g. small arena, player
        // standing centrally). Instead of always picking the single farthest
        // point, pick randomly among the top half farthest, so spawns still vary.
        List<Transform> sorted = new List<Transform>(spawnPoints);
        sorted.Sort((a, b) =>
            Vector3.Distance(b.position, playerPos).CompareTo(Vector3.Distance(a.position, playerPos)));

        int topHalfCount = Mathf.Max(1, sorted.Count / 2);
        return sorted[Random.Range(0, topHalfCount)];
    }

    // Enemy scripts should call this when an enemy dies.
    public void NotifyEnemyKilled()
    {
        if (!waveActive || IsGameOver) return;

        EnemiesRemaining = Mathf.Max(0, EnemiesRemaining - 1);
        int total = startingEnemyCount + CurrentWaveIndex * enemyCountIncreasePerWave;
        OnEnemiesLeftChanged?.Invoke(EnemiesRemaining, total);
    }

    // Call this from the upgrade popup's Start button to begin the next wave.
    public void StartNextWave()
    {
        if (!IsWaitingForNextWave || IsGameOver) return;
        IsWaitingForNextWave = false;
    }

    // Anything can trigger this - the countdown running out, or the player's health script on death.
    public void GameOver()
    {
        if (IsGameOver) return;

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.SaveHighScoreIfBeaten(); // NEW


        IsGameOver = true;
        waveActive = false;
        IsWaitingForNextWave = false;
        StopAllCoroutines();
        Debug.Log("[WaveManager] GAME OVER.");
        DeathScreenUI.Instance.ShowDeathScreen();

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            Destroy(playerObject);

        GameObject gun = GameObject.FindGameObjectWithTag("Weapon");
        if (gun != null)
            Destroy(gun);
    }
}