using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float health = 50f;
    private Rigidbody2D rb;

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
}
