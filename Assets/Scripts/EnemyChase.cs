using System.Collections.Generic;
using UnityEngine;

public class EnemyChase : MonoBehaviour
{
    public Rigidbody2D rb;
    public float speed = 3f;
    public string playerTag = "Player";

    [Header("Asteroid Avoidance")]
    public float avoidLookahead = 2.5f; // how far ahead to scan for asteroids in the way
    public float avoidMargin = 0.5f;    // extra clearance beyond an asteroid's own edge
    public float avoidStrength = 1.5f;  // how strongly avoidance steers relative to heading for the player

    [Header("Enemy Separation")]
    public float separationRadius = 0.9f; // other enemies closer than this get pushed away from
    public float separationStrength = 1f; // how strongly separation steers relative to heading for the player

    const float StuckCheckInterval = 0.5f;   // how often to check for progress
    const float StuckDistanceThreshold = 0.15f; // must move at least this far per check while touching something, or it counts as stuck
    const float EscapeDuration = 0.6f;       // how long to prioritize getting clear once stuck

    private Transform player;
    readonly Dictionary<Collider2D, Vector2> obstacleContactNormals = new Dictionary<Collider2D, Vector2>();

    Vector2 lastCheckPosition;
    float stuckCheckTimer;
    float escapeTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        lastCheckPosition = rb.position;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    void FixedUpdate()
    {
        if (player == null)
        {
            return;
        }

        UpdateStuckCheck();

        Vector2 velocity = Vector2.zero;
        if (escapeTimer > 0f)
        {
            escapeTimer -= Time.fixedDeltaTime;
            velocity = EscapeVelocity();
        }

        // Not escaping, or already clear of everything it was pushing away
        // from - resume the chase instead of ignoring the player for no reason.
        if (velocity == Vector2.zero)
        {
            escapeTimer = 0f;
            velocity = ChaseVelocity();
        }

        Vector2 targetPosition = rb.position + velocity * Time.fixedDeltaTime;
        rb.MovePosition(targetPosition);
    }

    // Normal behaviour: head for the player, steering around any asteroid
    // that's actually in the way before ever touching it, then sliding off
    // whatever it does end up touching - asteroid, other enemy, or the
    // player - instead of pinning against it.
    Vector2 ChaseVelocity()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        direction = (direction
            + AvoidanceSteering(direction) * avoidStrength
            + SeparationSteering() * separationStrength).normalized;

        Vector2 velocity = direction * speed;

        foreach (Vector2 normal in obstacleContactNormals.Values)
        {
            float into = Vector2.Dot(velocity, normal);
            if (into < 0f)
            {
                Vector2 tangent = new Vector2(-normal.y, normal.x);
                if (Vector2.Dot(direction, tangent) < 0f) tangent = -tangent;

                velocity -= into * normal;
                velocity += tangent * Mathf.Abs(into);
            }
        }

        return velocity;
    }

    // Looks ahead along the intended travel direction for any asteroid
    // whose actual footprint (read from its own collider bounds, so it
    // scales automatically with however big that particular asteroid is)
    // would block the path, and returns a sideways push to route around it.
    Vector2 AvoidanceSteering(Vector2 travelDir)
    {
        Vector2 origin = rb.position;
        Vector2 avoidance = Vector2.zero;

        Collider2D[] nearby = Physics2D.OverlapCircleAll(origin + travelDir * (avoidLookahead * 0.5f), avoidLookahead);
        foreach (Collider2D col in nearby)
        {
            if (col == null || !col.gameObject.name.StartsWith("Asteroid")) continue;

            Vector2 toAsteroid = (Vector2)col.bounds.center - origin;
            float alongPath = Vector2.Dot(toAsteroid, travelDir);
            if (alongPath <= 0f) continue; // behind us - not in the way

            float asteroidRadius = Mathf.Max(col.bounds.extents.x, col.bounds.extents.y);
            float clearance = asteroidRadius + avoidMargin;

            Vector2 lateral = toAsteroid - travelDir * alongPath; // perpendicular offset from our straight-line path
            float lateralDist = lateral.magnitude;
            if (lateralDist >= clearance) continue; // path already clears its footprint

            Vector2 pushDir = lateralDist > 0.01f ? -lateral.normalized : Vector2.Perpendicular(travelDir).normalized;
            avoidance += pushDir * ((clearance - lateralDist) / clearance);
        }

        return avoidance;
    }

    // Pushes away from other enemies crowding this one's immediate space, so
    // a whole group converging on the same gap between asteroids spreads out
    // and queues through it instead of piling on top of each other and
    // jamming solid.
    Vector2 SeparationSteering()
    {
        Vector2 push = Vector2.zero;

        Collider2D[] nearby = Physics2D.OverlapCircleAll(rb.position, separationRadius);
        foreach (Collider2D col in nearby)
        {
            if (col == null || col.gameObject == gameObject || !col.CompareTag("Enemy")) continue;

            Vector2 away = rb.position - (Vector2)col.transform.position;
            float dist = away.magnitude;
            if (dist < 0.01f) { away = Random.insideUnitCircle; dist = 0.01f; }

            push += away.normalized * (1f - Mathf.Clamp01(dist / separationRadius));
        }

        return push;
    }

    // Stuck behaviour: ignore the player entirely and push straight away
    // from whatever it's wedged against (asteroids, other enemies, or the player).
    Vector2 EscapeVelocity()
    {
        Vector2 escapeDir = Vector2.zero;
        foreach (Vector2 normal in obstacleContactNormals.Values)
            escapeDir += normal;

        return escapeDir.sqrMagnitude > 0.0001f ? escapeDir.normalized * speed : Vector2.zero;
    }

    // Toggles off chasing the player for a short window whenever it's been
    // pressed against something without making real progress - handles cases
    // the tangent-slide in ChaseVelocity can still get pinned by, like being
    // wedged between two asteroids (or other enemies) with cancelling tangents.
    void UpdateStuckCheck()
    {
        if (escapeTimer > 0f) return; // already escaping, don't re-trigger mid-escape

        stuckCheckTimer += Time.fixedDeltaTime;
        if (stuckCheckTimer < StuckCheckInterval) return;

        float moved = Vector2.Distance(rb.position, lastCheckPosition);
        stuckCheckTimer = 0f;
        lastCheckPosition = rb.position;

        if (moved < StuckDistanceThreshold && obstacleContactNormals.Count > 0)
            escapeTimer = EscapeDuration;
    }

    void OnCollisionEnter2D(Collision2D collision) => TrackObstacleContact(collision);
    void OnCollisionStay2D(Collision2D collision) => TrackObstacleContact(collision);

    void OnCollisionExit2D(Collision2D collision)
    {
        obstacleContactNormals.Remove(collision.collider);
    }

    // Tracks contact with anything solid worth sliding off of instead of
    // sticking to - asteroids, other enemies, and the player. The same
    // tangent-slide in ChaseVelocity handles all of them uniformly, so
    // bumping into another enemy or the player glances off just like an
    // asteroid edge instead of jittering/sticking.
    void TrackObstacleContact(Collision2D collision)
    {
        bool isAsteroid = collision.gameObject.name.StartsWith("Asteroid");
        bool isEnemyOrPlayer = collision.gameObject.CompareTag("Enemy") || collision.gameObject.CompareTag(playerTag);
        if (!isAsteroid && !isEnemyOrPlayer) return;

        Vector2 sum = Vector2.zero;
        int count = collision.contactCount;
        for (int i = 0; i < count; i++) sum += collision.GetContact(i).normal;

        if (count > 0) obstacleContactNormals[collision.collider] = (sum / count).normalized;
    }
}
