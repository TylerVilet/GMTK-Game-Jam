using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 1.5f;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private Transform enemy; // drag the enemy object in here (the thing this gun orbits)
    [SerializeField] private float orbitRadius = 1f; // how far the gun sits from the enemy
    [SerializeField] private float detectionRange = 12f; // only fires if the player is within this range

    public float damageMultiplier = 1f;
    public float rangeMultiplier = 1f;

    private float fireTimer = 0f;
    private Transform player;

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;
    }

    void Update()
    {
        if (player == null || enemy == null) return;

        OrbitAndAim();

        fireTimer += Time.deltaTime;

        float distanceToPlayer = Vector2.Distance(enemy.position, player.position);
        if (distanceToPlayer <= detectionRange && fireTimer >= fireRate)
        {
            Shoot();
            fireTimer = 0f;
        }
    }

    void OrbitAndAim()
    {
        Vector2 direction = ((Vector2)player.position - (Vector2)enemy.position).normalized;

        // Position the gun around the enemy, offset toward the player
        transform.position = (Vector2)enemy.position + direction * orbitRadius;

        // Rotate the gun to face outward (toward the player)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }

    void Shoot()
    {
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        Rigidbody2D projRb = projectile.GetComponent<Rigidbody2D>();
        if (projRb != null)
            projRb.linearVelocity = firePoint.up * projectileSpeed;

        EnemyShoot bulletScript = projectile.GetComponent<EnemyShoot>();
        if (bulletScript != null)
        {
            bulletScript.damage *= damageMultiplier;
            bulletScript.maxRange *= rangeMultiplier;
        }
    }
}