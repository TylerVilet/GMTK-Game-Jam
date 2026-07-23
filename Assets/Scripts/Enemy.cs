using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float health = 50f;
    private Rigidbody2D rb;
    public float damage = 10f;
    public float damageCooldown = 0.5f;
    private float lastHitTime = -10f;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
    }

    private void FixedUpdate()
    {
        // death
        if (health <= 0)
        {
            Destroy(gameObject);

        }
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        Debug.Log("Health: " + health);
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Collided with Player");

            Player player = collision.gameObject.GetComponent<Player>();
            if (player != null && Time.time >= damageCooldown + lastHitTime)
            {
                player.loseHealth(damage);
                lastHitTime = Time.time;
            }
        }
    }
}
