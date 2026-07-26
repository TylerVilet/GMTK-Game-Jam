using System.Collections.Generic;
using UnityEngine;

public class EnemyShoot : MonoBehaviour
{
    public float damage = 10f;
    public float maxRange = 15f;
    private Vector3 spawnPosition;
    private Collider2D myCollider;

    // Reflected bullets need to hit enemies, so we keep track of the
    // enemy colliders we ignored at spawn (to stop this bullet hitting its
    // own shooter) in order to re-enable collision with them on Reflect().
    private readonly List<Collider2D> ignoredEnemyColliders = new List<Collider2D>();
    private bool reflected;

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
            {
                Physics2D.IgnoreCollision(myCollider, enemyCollider);
                ignoredEnemyColliders.Add(enemyCollider);
            }
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

    // Called by SwordWeapon when the player parries this bullet - sends it
    // back out toward enemies instead of the player, dealing the sword's own
    // reflect damage rather than whatever the original bullet would have dealt.
    public void Reflect(Vector2 direction, float speed, float reflectDamage)
    {
        reflected = true;
        damage = reflectDamage;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = direction.normalized * speed;

        transform.up = direction; // face the new travel direction

        // Re-enable collision with enemies now that this bullet is meant to hurt them
        foreach (Collider2D enemyCollider in ignoredEnemyColliders)
        {
            if (enemyCollider != null)
                Physics2D.IgnoreCollision(myCollider, enemyCollider, false);
        }

        spawnPosition = transform.position; // reset range so the reflect doesn't cut its flight short
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
        if (reflected)
        {
            if (hitObject.CompareTag("Enemy"))
            {
                Enemy enemy = hitObject.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                }
                Destroy(gameObject);
            }
            else if (hitObject.name.StartsWith("Wall") || hitObject.name.StartsWith("Asteroid"))
            {
                Destroy(gameObject);
            }
            return; // a reflected bullet can no longer hurt the player
        }

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