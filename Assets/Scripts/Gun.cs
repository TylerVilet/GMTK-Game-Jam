using UnityEngine;
using UnityEngine.InputSystem;

public class Gun : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.25f;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private Transform player; // drag your player object in here
    [SerializeField] private float orbitRadius = 1f; // how far the gun sits from the player

    // Applied to each spawned bullet - power-ups multiply these instead of
    // touching the bullet prefab's own base values.
    public float damageMultiplier = 1f;
    public float rangeMultiplier = 1f;

    private float fireTimer = 0f;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        OrbitAndAim();

        fireTimer += Time.deltaTime;

        if (Mouse.current.leftButton.isPressed && fireTimer >= fireRate)
        {
            Shoot();
            fireTimer = 0f;
        }
    }

    void OrbitAndAim()
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(
            new Vector3(mouseScreenPos.x, mouseScreenPos.y, mainCamera.nearClipPlane)
        );

        Vector2 direction = ((Vector2)mouseWorldPos - (Vector2)player.position).normalized;

        // Position the gun around the player, offset toward the mouse
        transform.position = (Vector2)player.position + direction * orbitRadius;

        // Rotate the gun to face outward (same direction it's orbiting toward)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }

    void Shoot()
    {
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        Rigidbody2D projRb = projectile.GetComponent<Rigidbody2D>();
        projRb.linearVelocity = firePoint.up * projectileSpeed;

        Bullet bulletScript = projectile.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.damage *= damageMultiplier;
            bulletScript.maxRange *= rangeMultiplier;
        }
    }

    // percent as a fraction, e.g. 0.25 for +25% fire rate (shoots more often,
    // so this shortens the cooldown rather than lengthening it).
    public void IncreaseFireRate(float percent)
    {
        fireRate /= 1f + percent;
    }

    public void EnableGun()
    {
        enabled = true;
    }
}