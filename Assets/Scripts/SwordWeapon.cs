using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Orbits the player like Gun does, but instead of firing projectiles it
// swings through an arc on click - anything caught in the blade's path
// during that swing takes damage once, not once per frame it overlaps.
public class SwordWeapon : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float orbitRadius = 1f;
    [SerializeField] private Collider2D bladeCollider; // trigger collider on the blade child

    [Header("Swing")]
    [SerializeField] private float damage = 15f;
    [SerializeField] private float swingArc = 100f;     // total degrees swept per swing
    [SerializeField] private float swingDuration = 0.18f;
    [SerializeField] private float swingCooldown = 0.4f;

    private Camera mainCamera;
    private float cooldownTimer;
    private bool isSwinging;
    private readonly HashSet<Collider2D> hitThisSwing = new HashSet<Collider2D>();

    void Start()
    {
        mainCamera = Camera.main;
        if (bladeCollider != null) bladeCollider.enabled = false; // only "live" mid-swing, so idle sword can't damage anything
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
        if (!isSwinging) AimTowardMouse();

        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        if (!isSwinging && cooldownTimer <= 0f && Mouse.current.leftButton.wasPressedThisFrame)
        {
            StartCoroutine(Swing());
        }
    }

    void AimTowardMouse()
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(
            new Vector3(mouseScreenPos.x, mouseScreenPos.y, mainCamera.nearClipPlane)
        );

        Vector2 direction = ((Vector2)mouseWorldPos - (Vector2)player.position).normalized;
        Player.AimDirection = direction;

        transform.position = (Vector2)player.position + direction * orbitRadius;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }

    IEnumerator Swing()
    {
        isSwinging = true;
        hitThisSwing.Clear();
        if (bladeCollider != null) bladeCollider.enabled = true;

        float centerAngle = transform.eulerAngles.z;
        float startAngle = centerAngle - swingArc * 0.5f;
        float endAngle = centerAngle + swingArc * 0.5f;

        float elapsed = 0f;
        while (elapsed < swingDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / swingDuration);
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.LerpAngle(startAngle, endAngle, t));
            yield return null;
        }

        if (bladeCollider != null) bladeCollider.enabled = false;
        isSwinging = false;
        cooldownTimer = swingCooldown;
    }

    void OnTriggerEnter2D(Collider2D other) => TryHit(other);
    void OnTriggerStay2D(Collider2D other) => TryHit(other);

    void TryHit(Collider2D other)
    {
        if (!other.CompareTag("Enemy") || hitThisSwing.Contains(other)) return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null) return;

        enemy.TakeDamage(damage);
        hitThisSwing.Add(other);
    }
}
