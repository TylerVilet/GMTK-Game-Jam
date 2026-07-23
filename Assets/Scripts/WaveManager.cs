using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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

    [Header("Events (hook UI / other systems to these)")]
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
    }

    void Start()
    {
        StartCoroutine(RunWaves());
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
            OnWaveCleared?.Invoke();

            IsWaitingForNextWave = true;
            yield return new WaitUntil(() => !IsWaitingForNextWave || IsGameOver);

            if (IsGameOver) yield break;
        }
    }

    IEnumerator RunWave()
    {
        int enemyCount = startingEnemyCount + CurrentWaveIndex * enemyCountIncreasePerWave;
        BuildEligiblePrefabs();

        OnWaveStarted?.Invoke(CurrentWaveIndex);
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

    void SpawnEnemy()
    {
        if (eligiblePrefabs.Count == 0) return;
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        GameObject prefab = eligiblePrefabs[Random.Range(0, eligiblePrefabs.Count)];
        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject enemy = Instantiate(prefab, point.position, point.rotation);

        if (!enemy.CompareTag(enemyTag))
            enemy.tag = enemyTag;
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

        IsGameOver = true;
        waveActive = false;
        IsWaitingForNextWave = false;
        StopAllCoroutines();
        Debug.Log("[WaveManager] GAME OVER.");
        OnGameOver?.Invoke();
    }
}
