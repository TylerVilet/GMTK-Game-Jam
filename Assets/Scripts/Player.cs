using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
public class Player : MonoBehaviour
{

    public Rigidbody2D rb;
    public float speed = 5f;
    public float maxHealth = 30f;
    public float health = 30f;
    public float dashDistance = 0.5f;

    [Header("Dash")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 5f;

    float dashCooldownTimer = 0f;
    float dashTimeRemaining = 0f;
    Vector2 dashDirection;
    bool isDashing = false;
    bool dashQueued = false;


    public TMP_Text healthText;


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        healthText.text = health + "/" + maxHealth;
    }

    private void Update()
    {
        if (Keyboard.current.leftShiftKey.wasPressedThisFrame) dashQueued = true;
    }

    void FixedUpdate()
    {
        health = Mathf.Clamp(health, 0f, maxHealth);
        if (health <= 0f && WaveManager.Instance != null)
        {
            WaveManager.Instance.GameOver();
        }

        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.fixedDeltaTime;

        Vector2 direction = moveForward() + moveDown() + moveLeft() + moveRight();

        // Start a dash if Shift is pressed, off cooldown, and not already dashing
        if (!isDashing && dashCooldownTimer <= 0f && dashQueued)
        {
            Vector2 dashDir = direction.sqrMagnitude > 0f ? direction.normalized : (Vector2)transform.up;
            StartDash(dashDir);
        }
        dashQueued = false;


        if (isDashing)
        {
            Debug.Log("Dashed");
            rb.linearVelocity = dashDirection * dashSpeed;
            dashTimeRemaining -= Time.fixedDeltaTime;
            if (dashTimeRemaining <= 0f)
                isDashing = false;
        }
        else
        {
            rb.linearVelocity = direction.normalized * speed;
        }
    }

    void StartDash(Vector2 direction)
    {
        isDashing = true;
        dashDirection = direction;
        dashTimeRemaining = dashDuration;
        dashCooldownTimer = dashCooldown;
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

        healthText.text = health + "/" + maxHealth; 
    }
    public void EnableMovement()
    {
        enabled = true;
    }

    public void updateHealthUI()
    {
        healthText.text = health + "/" + maxHealth;
    }
}