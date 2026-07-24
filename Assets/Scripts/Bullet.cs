using UnityEngine;

public class Bullet : MonoBehaviour
{

    public float damage = 10f;
    public float maxRange = 15f;

    private Vector3 spawnPosition;

    private void Start()
    {
        spawnPosition = transform.position;
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
        if (collision.gameObject.CompareTag("Enemy")) {
            Debug.Log("Hit enemy");

            // get script of gameobject
            Enemy enemy = collision.gameObject.GetComponent<Enemy>();
            if (enemy != null) {
                enemy.TakeDamage(damage);
            }
            Destroy(gameObject); // destroy the bullet on hit
        }
        else if (collision.gameObject.name.StartsWith("Wall") || collision.gameObject.name.StartsWith("Asteroid"))
        {
            Destroy(gameObject); // destroy the bullet when it hits a border wall or blockade
        }
        else if (collision.gameObject.CompareTag("EnemyBullet"))
        {
            // destroy player and enemy bullet if they collide
            Destroy(gameObject);
            Destroy(collision.gameObject);
        }
    }

    private void OnBecameInvisible()
    {
        Destroy(gameObject);
    }

}
