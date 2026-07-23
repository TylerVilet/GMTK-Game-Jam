using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float health = 50f;


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
