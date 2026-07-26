using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Orbits the player like Gun does, but instead of firing projectiles it
// stabs outward on click (or held click) - a quick lunge along the aim
// direction and back, rather than a rotating swing. Anything caught in the
// blade's path takes damage once per stab, not once per frame it overlaps.
// The lunge also cuts short and retracts immediately if it hits an asteroid
// or wall instead of passing through it.
public class SwordWeapon : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float orbitRadius = 1f; // resting distance from the player
    [SerializeField] private Collider2D bladeCollider; // trigger collider on the blade child

    [Header("Stab")]
    [SerializeField] private float damage = 20f;
    [SerializeField] private float stabDistance = 2.2f; // how much further out the blade lunges, beyond orbitRadius
    [SerializeField] private float stabOutDuration = 0.035f;  // quick thrust out
    [SerializeField] private float stabHoldDuration = 0.015f; // brief hold at full extension so the hit reliably registers
    [SerializeField] private float stabReturnDuration = 0.05f; // ease back to resting distance
    [SerializeField] private float stabCooldown = 0.15f;

    [Header("Reflect")]
    [SerializeField] private float reflectWidth = 3f;     // how far the blocking swing reaches, perpendicular to aim - the "width" of the reflection
    [SerializeField] private float reflectThickness = 0.5f; // how deep the blocking zone is along the aim axis
    [SerializeField] private float reflectDuration = 1f;   // how long the perpendicular block stance holds
    [SerializeField] private float reflectSpeed = 14f;     // speed the parried bullet is sent back out at
    [SerializeField] private float reflectDamage = 25f;    // damage the parried bullet deals to enemies it hits
    [SerializeField] private float reflectCooldown = 0.4f;

    // Applied on top of the base damage/cooldown above - power-ups multiply
    // these instead of touching the base values directly, same pattern as Gun.
    public float damageMultiplier = 1f;

    private Camera mainCamera;
    private float cooldownTimer;
    private float reflectCooldownTimer;
    private bool isStabbing;
    private bool isReflecting;
    private bool hitObstacle;
    private Vector2 lastAimDirection = Vector2.right;
    private readonly HashSet<Collider2D> hitThisStab = new HashSet<Collider2D>();
    private readonly HashSet<EnemyShoot> reflectedThisSwing = new HashSet<EnemyShoot>();

    void Start()
    {
        mainCamera = Camera.main;
        if (bladeCollider != null) bladeCollider.enabled = false; // only "live" mid-stab, so an idle sword can't damage anything
    }

    // Called by Player.EquipSelectedWeapon after this prefab is instantiated -
    // the weapon prefabs are spawned standalone (not parented), so there's no
    // scene-level reference to wire up in the Inspector like there used to be.
    public void SetPlayer(Transform playerTransform)
    {
        player = playerTransform;
    }

    void Update()
    {
        if (!isStabbing && !isReflecting) AimTowardMouse();
        // else: position/rotation are fully driven by Stab()/ReflectSwing() below.

        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
        if (reflectCooldownTimer > 0f) reflectCooldownTimer -= Time.deltaTime;

        // Held (not just pressed) so a held-down button keeps stabbing as
        // fast as the cooldown allows, same as the gun's fire input.
        if (!isStabbing && !isReflecting && cooldownTimer <= 0f && Mouse.current.leftButton.isPressed)
        {
            StartCoroutine(Stab());
        }

        // Reflect is a discrete parry (rebindable in Settings, right-click by
        // default) - one press turns the blade perpendicular to hold a block
        // stance for the full swing duration.
        SettingsManager.InputBinding reflectBinding = SettingsManager.Instance != null
            ? SettingsManager.Instance.ReflectBinding
            : SettingsManager.InputBinding.FromMouseButton(1);
        if (!isStabbing && !isReflecting && reflectCooldownTimer <= 0f && reflectBinding.WasPressedThisFrame())
        {
            StartCoroutine(ReflectSwing());
        }
    }

    // Turns the blade perpendicular to the current aim direction and holds it
    // there as a wide block for reflectDuration. Checked fresh every frame
    // (not just once) so it catches bullets from enemies that spawn mid-swing
    // too, not just whatever already existed when the swing started.
    IEnumerator ReflectSwing()
    {
        isReflecting = true;
        reflectCooldownTimer = reflectCooldown;
        reflectedThisSwing.Clear();

        Vector2 direction = lastAimDirection;
        Vector2 perpendicular = new Vector2(-direction.y, direction.x);
        float angle = Mathf.Atan2(perpendicular.y, perpendicular.x) * Mathf.Rad2Deg;
        Vector2 boxSize = new Vector2(reflectWidth, reflectThickness);

        float elapsed = 0f;
        while (elapsed < reflectDuration)
        {
            elapsed += Time.deltaTime;

            Vector2 center = (Vector2)player.position + direction * orbitRadius;
            transform.position = center;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);

            Collider2D[] hits = Physics2D.OverlapBoxAll(center, boxSize, angle);
            foreach (Collider2D hit in hits)
            {
                EnemyShoot enemyBullet = hit.GetComponent<EnemyShoot>();
                if (enemyBullet != null && !reflectedThisSwing.Contains(enemyBullet))
                {
                    enemyBullet.Reflect(direction, reflectSpeed, reflectDamage);
                    reflectedThisSwing.Add(enemyBullet);
                }
            }

            yield return null;
        }

        isReflecting = false;
    }

    void AimTowardMouse()
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(
            new Vector3(mouseScreenPos.x, mouseScreenPos.y, mainCamera.nearClipPlane)
        );

        Vector2 direction = ((Vector2)mouseWorldPos - (Vector2)player.position).normalized;
        lastAimDirection = direction;
        Player.AimDirection = direction;

        transform.position = (Vector2)player.position + direction * orbitRadius;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }

    IEnumerator Stab()
    {
        isStabbing = true;
        hitThisStab.Clear();
        hitObstacle = false;
        if (bladeCollider != null) bladeCollider.enabled = true;

        // Direction and rotation lock in for the whole stab - a thrust
        // doesn't change heading mid-motion like the old rotating swing did.
        Vector2 direction = lastAimDirection;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);

        float nearRadius = orbitRadius;
        float farRadius = orbitRadius + stabDistance;
        float reachedRadius = farRadius;

        // Lunge outward, but the instant the blade touches an asteroid or
        // wall, stop advancing right there (not passing through it) and skip
        // straight to retracting instead of continuing to full extension.
        float elapsed = 0f;
        while (elapsed < stabOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / stabOutDuration);
            float radius = Mathf.Lerp(nearRadius, farRadius, t);
            transform.position = (Vector2)player.position + direction * radius;

            if (hitObstacle)
            {
                reachedRadius = radius;
                break;
            }

            yield return null;
        }

        if (!hitObstacle)
        {
            float held = 0f;
            while (held < stabHoldDuration)
            {
                held += Time.deltaTime;
                transform.position = (Vector2)player.position + direction * farRadius;
                yield return null;
            }
        }

        yield return LungeTo(direction, reachedRadius, nearRadius, stabReturnDuration);

        if (bladeCollider != null) bladeCollider.enabled = false;
        isStabbing = false;
        cooldownTimer = stabCooldown;
    }

    IEnumerator LungeTo(Vector2 direction, float fromRadius, float toRadius, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float radius = Mathf.Lerp(fromRadius, toRadius, t);
            transform.position = (Vector2)player.position + direction * radius;
            yield return null;
        }
    }

    void OnTriggerEnter2D(Collider2D other) => TryHit(other);
    void OnTriggerStay2D(Collider2D other) => TryHit(other);

    void TryHit(Collider2D other)
    {
        if (other.gameObject.name.StartsWith("Asteroid") || other.gameObject.name.StartsWith("Wall"))
        {
            hitObstacle = true;
            return;
        }

        if (!other.CompareTag("Enemy") || hitThisStab.Contains(other)) return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null) return;

        enemy.TakeDamage(damage * damageMultiplier);
        hitThisStab.Add(other);
    }

    // percent as a fraction, e.g. 0.25 for +25% fire rate (swings more often,
    // so this shortens the cooldown rather than lengthening it) - mirrors
    // Gun.IncreaseFireRate so the same "fire rate" power-up works on either weapon.
    public void IncreaseFireRate(float percent)
    {
        stabCooldown /= 1f + percent;
    }
}
