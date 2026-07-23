using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    
    public Rigidbody2D rb;
    public float speed = 5f;
    public float maxHealth = 30f;
    public float health = 30f;


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
    }

    void FixedUpdate()
    {
        health = Mathf.Clamp(health, 0f, maxHealth);

        if (health <= 0f && WaveManager.Instance != null)
        {
            WaveManager.Instance.GameOver();
        }

        Vector2 direction = moveForward() + moveDown() + moveLeft() + moveRight();
        rb.linearVelocity = direction.normalized * speed;
    }

    Vector2 moveForward()
    {
        return Keyboard.current.wKey.isPressed ? (Vector2)transform.up : Vector2.zero;
    }
    Vector2 moveDown()
    {
        return Keyboard.current.sKey.isPressed ? -(Vector2)transform.up : Vector2.zero;
    }
    Vector2 moveLeft()
    {
        return Keyboard.current.aKey.isPressed ? -(Vector2)transform.right : Vector2.zero;
    }
    Vector2 moveRight()
    {
        return Keyboard.current.dKey.isPressed ? (Vector2)transform.right : Vector2.zero;
    }

    public void loseHealth(float damage)
    {
        health = Mathf.Clamp(health - damage, 0f, maxHealth);
        Debug.Log("Player health: " + health);

        if (health <= 0f && WaveManager.Instance != null)
        {
            WaveManager.Instance.GameOver();
        }
    }
}
