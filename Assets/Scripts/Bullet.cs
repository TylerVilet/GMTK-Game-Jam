using UnityEngine;

public class Bullet : MonoBehaviour
{

    public float damage = 10f;
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
    }

    private void OnBecameInvisible()
    {
        Destroy(gameObject);
    }

}
