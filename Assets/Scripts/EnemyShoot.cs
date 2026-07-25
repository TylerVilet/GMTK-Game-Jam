using UnityEngine;

public class EnemyShoot : MonoBehaviour
{
    public float damage = 10f;
    public float maxRange = 15f;
    private Vector3 spawnPosition;
    private Collider2D myCollider;

    private void Start()
    {
        spawnPosition = transform.position;
        myCollider = GetComponent<Collider2D>();

        // Ignore collisions with all other enemies and enemy bullets
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var enemy in enemies)
        {
            Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
            if (enemyCollider != null)
                Physics2D.IgnoreCollision(myCollider, enemyCollider);
        }
        GameObject[] bullets = GameObject.FindGameObjectsWithTag("EnemyBullet");
        foreach (var otherBullet in bullets)
        {
            if (otherBullet == gameObject) continue;
            Collider2D bulletCollider = otherBullet.GetComponent<Collider2D>();
            if (bulletCollider != null)
                Physics2D.IgnoreCollision(myCollider, bulletCollider);
        }
    }

    private void Update()
    {
        if (Vector3.Distance(spawnPosition, transform.position) >= maxRange)
        {
            Destroy(gameObject);
        }
    }

    // Fires for regular (solid collider) enemy bullets
    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision.gameObject);
    }

    // Fires for sniper (trigger collider) enemy bullets
    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other.gameObject);
    }

    private void HandleHit(GameObject hitObject)
    {
        if (hitObject.CompareTag("Player"))
        {
            Debug.Log("Hit Player");
            Player player = hitObject.GetComponent<Player>();
            if (player != null)
            {
                player.loseHealth(damage);
            }
            Destroy(gameObject); // destroy the bullet on hit
        }
        else if (hitObject.name.StartsWith("Wall") || hitObject.name.StartsWith("Asteroid"))
        {
            Destroy(gameObject); // destroy the bullet when it hits a border wall or asteroid
        }
    }

    private void OnBecameInvisible()
    {
        Destroy(gameObject);
    }
}