using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static StartScreenUI;
public class Player : MonoBehaviour
{

    // Read by CharacterVisual so the player faces the same way as whichever
    // weapon is currently active (set by that weapon, not by Player itself).
    public static Vector2 AimDirection { get; set; } = Vector2.right;

    public Rigidbody2D rb;
    public float speed = 5f;
    public float maxHealth = 30f;
    public float health = 30f;
    public float dashDistance = 0.5f;

    readonly Dictionary<Collider2D, Vector2> blockadeContactNormals = new Dictionary<Collider2D, Vector2>();

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
    public Image healthBarFill;


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        RefreshHealthUI();

        EquipSelectedWeapon();
        ApplySelectedCharacterVisual();
    }

    void ApplySelectedCharacterVisual()
    {
        CharacterVisual visual = GetComponentInChildren<CharacterVisual>();
        if (visual == null) return;

        if (GameSelection.SelectedCharacterLeftSprite != null)
            visual.leftSprite = GameSelection.SelectedCharacterLeftSprite;
        if (GameSelection.SelectedCharacterRightSprite != null)
            visual.rightSprite = GameSelection.SelectedCharacterRightSprite;
    }

    void EquipSelectedWeapon()
    {
        if (GameSelection.SelectedWeaponPrefab == null) return;

        GameObject weaponObject = Instantiate(GameSelection.SelectedWeaponPrefab);

        Gun gun = weaponObject.GetComponent<Gun>();
        if (gun != null)
        {
            gun.SetPlayer(transform);
            return;
        }

        SwordWeapon sword = weaponObject.GetComponent<SwordWeapon>();
        if (sword != null)
        {
            sword.SetPlayer(transform);
        }
    }

    private void Update()
    {
        Key dashKey = SettingsManager.Instance != null ? SettingsManager.Instance.DashKey : Key.LeftShift;
        if (Keyboard.current[dashKey].wasPressedThisFrame) dashQueued = true;
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
            rb.linearVelocity = SlideAlongBlockades(direction.normalized * speed);
        }
    }

    // Pressing into a blockade's edge slides the velocity along it instead of
    // just stopping dead against it.
    Vector2 SlideAlongBlockades(Vector2 velocity)
    {
        foreach (Vector2 normal in blockadeContactNormals.Values)
        {
            float into = Vector2.Dot(velocity, normal);
            if (into < 0f) velocity -= into * normal;
        }
        return velocity;
    }

    void OnCollisionEnter2D(Collision2D collision) => TrackBlockadeContact(collision);
    void OnCollisionStay2D(Collision2D collision) => TrackBlockadeContact(collision);

    void OnCollisionExit2D(Collision2D collision)
    {
        blockadeContactNormals.Remove(collision.collider);
    }

    void TrackBlockadeContact(Collision2D collision)
    {
        if (!collision.gameObject.name.StartsWith("Asteroid")) return;

        Vector2 sum = Vector2.zero;
        int count = collision.contactCount;
        for (int i = 0; i < count; i++) sum += collision.GetContact(i).normal;

        if (count > 0) blockadeContactNormals[collision.collider] = (sum / count).normalized;
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

        RefreshHealthUI();
    }
    public void EnableMovement()
    {
        enabled = true;
    }

    public void updateHealthUI()
    {
        RefreshHealthUI();
    }

    void RefreshHealthUI()
    {
        healthText.text = health + "/" + maxHealth;
        if (healthBarFill != null)
            healthBarFill.fillAmount = maxHealth > 0f ? health / maxHealth : 0f;
    }
}