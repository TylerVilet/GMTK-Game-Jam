using UnityEngine;

public class EnemyShoot : MonoBehaviour
{

    public float damage = 10f;
    public float maxRange = 15f;

    private Vector3 spawnPosition;

    private void Start()
    {
        spawnPosition = transform.position;

        // Ignore collisions with all other enemies and enemy bullets
        Collider2D myCollider = GetComponent<Collider2D>();
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
                Physics2D.IgnoreCollision(GetComponent<Collider2D>(), bulletCollider);
        }
    }

    private void Update()
    {
        if (Vector3.Distance(spawnPosition, transform.position) >= maxRange)
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Hit Player");

            // get script of gameobject
            Player player = collision.gameObject.GetComponent<Player>();
            if (player != null)
            {
                player.loseHealth(damage);
            }
            Destroy(gameObject); // destroy the bullet on hit
        }
        else if (collision.gameObject.name.StartsWith("Wall"))
        {
            Destroy(gameObject); // destroy the bullet when it hits a border wall
        }
    }

    private void OnBecameInvisible()
    {
        Destroy(gameObject);
    }

}
