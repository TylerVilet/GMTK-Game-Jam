using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// Talks to a Dreamlo (dreamlo.com) leaderboard - a free, no-signup service
// built for exactly this (game jam global leaderboards). All calls are plain
// HTTP GETs, so no SDK/auth is needed.
//
// NOTE: this leaderboard's free tier returns "ERROR:SSL not enabled for this
// leaderboard." on https:// - confirmed by hand with curl - so direct calls
// must stay http://, not https://. That breaks a WebGL build hosted over
// https (e.g. itch.io): browsers block an https page from making plain http
// requests (mixed content). proxyBaseUrl works around this - see below.
//
// NOTE: like any client-only leaderboard, the private key ships inside the
// build and a determined player could forge scores by hitting the API
// directly. That's an accepted tradeoff for a jam leaderboard, not something
// this fixes - there's no server of ours to validate submissions against.
public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    [Header("Dreamlo keys (from dreamlo.com - Private Code is add-only, don't confuse the two)")]
    [SerializeField] private string dreamloPrivateCode = "AWixELjFKEaP_Ved-_XQ1gwwIRY3IadE2MY7tIWoiCeg";
    [SerializeField] private string dreamloPublicCode = "6a658bfd8f40bb12187bb31c";

    [Header("Optional https relay (tools/dreamlo-proxy-worker.js) - required for itch.io/WebGL, leave blank for Editor/desktop")]
    [SerializeField] private string proxyBaseUrl = "https://green-breeze-ddc4.rylanlottes.workers.dev";

    // http://dreamlo.com when no relay is configured, otherwise the relay's
    // own https:// base - either way callers just get "/lb/..." appended.
    string DreamloBaseUrl => string.IsNullOrEmpty(proxyBaseUrl) ? "http://dreamlo.com" : proxyBaseUrl.TrimEnd('/');

    const string PlayerNameKey = "PlayerName";
    const string DefaultPlayerName = "Player";
    const int MaxPlayerNameLength = 10;

    public struct LeaderboardEntry
    {
        public string name;
        public int score;
    }

    public string PlayerName
    {
        get => PlayerPrefs.GetString(PlayerNameKey, DefaultPlayerName);
        set
        {
            string sanitized = string.IsNullOrWhiteSpace(value) ? DefaultPlayerName : value.Trim().Replace("|", "");
            if (sanitized.Length > MaxPlayerNameLength)
                sanitized = sanitized.Substring(0, MaxPlayerNameLength);
            PlayerPrefs.SetString(PlayerNameKey, sanitized);
            PlayerPrefs.Save();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        GameObject root = new GameObject("LeaderboardManager");
        DontDestroyOnLoad(root);
        root.AddComponent<LeaderboardManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Fire-and-forget - call this once, right after confirming a run beat the
    // player's previous personal best (not on every score change).
    public void SubmitScore(int score)
    {
        if (string.IsNullOrEmpty(dreamloPrivateCode))
        {
            Debug.LogWarning("[LeaderboardManager] No Dreamlo private code configured - skipping score submission.");
            return;
        }

        StartCoroutine(SubmitScoreRoutine(PlayerName, score));
    }

    IEnumerator SubmitScoreRoutine(string playerName, int score)
    {
        string url = $"{DreamloBaseUrl}/lb/{dreamloPrivateCode}/add/{UnityWebRequest.EscapeURL(playerName)}/{score}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
                Debug.LogWarning($"[LeaderboardManager] Score submission failed: {request.error}");
        }
    }

    // count <= 0 fetches everything Dreamlo has.
    public void FetchTopScores(int count, System.Action<List<LeaderboardEntry>> onComplete)
    {
        if (string.IsNullOrEmpty(dreamloPublicCode))
        {
            Debug.LogWarning("[LeaderboardManager] No Dreamlo public code configured.");
            onComplete?.Invoke(new List<LeaderboardEntry>());
            return;
        }

        StartCoroutine(FetchTopScoresRoutine(count, onComplete));
    }

    IEnumerator FetchTopScoresRoutine(int count, System.Action<List<LeaderboardEntry>> onComplete)
    {
        // Pipe format ("name|score|seconds|text|date" per line) rather than JSON -
        // Dreamlo's JSON collapses a single entry to an object instead of a
        // one-item array, which trips up strict parsers. Entries already come
        // back sorted highest-score-first, so we just take the first `count`.
        string url = $"{DreamloBaseUrl}/lb/{dreamloPublicCode}/pipe";

        List<LeaderboardEntry> entries = new List<LeaderboardEntry>();

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[LeaderboardManager] Fetch failed: {request.error}");
                onComplete?.Invoke(entries);
                yield break;
            }

            string[] lines = request.downloadHandler.text.Split('\n');
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] fields = line.Split('|');
                if (fields.Length < 2) continue;
                if (!int.TryParse(fields[1], out int score)) continue;

                entries.Add(new LeaderboardEntry { name = fields[0], score = score });

                if (count > 0 && entries.Count >= count) break;
            }
        }

        onComplete?.Invoke(entries);
    }
}
