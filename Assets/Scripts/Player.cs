using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    
    public Rigidbody2D rb;
    public float speed = 5f;
    public float health = 100f;


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // dead
        if (health <= 0)
        {
            WaveManager.Instance.GameOver();
        }

        moveForward();
        moveRight();
        moveLeft();
        moveDown();
    }

    void moveForward()
    {
        if (Keyboard.current.wKey.isPressed)
        {
            transform.position += transform.up * speed * Time.deltaTime;
        }
    }
    void moveDown()
    {
        if (Keyboard.current.sKey.isPressed)
        {
            transform.position += - transform.up * speed * Time.deltaTime;
        }
    }
    void moveLeft()
    {
        if (Keyboard.current.aKey.isPressed)
        {
            transform.position += - transform.right * speed * Time.deltaTime;
        }
    }
    void moveRight()
    {
        if (Keyboard.current.dKey.isPressed)
        {
            transform.position += transform.right * speed * Time.deltaTime;
        }
    }

    public void loseHealth(float damage)
    {
        health -= damage;
        Debug.Log("Player health: " + health);
    }
}
