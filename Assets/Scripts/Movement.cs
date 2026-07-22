using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{
    
    public Rigidbody2D rb;
    public float speed = 5f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
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
}
