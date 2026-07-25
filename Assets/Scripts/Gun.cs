using System.Collections.Generic;
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
    [SerializeField] private SpriteRenderer visualRenderer;

    [Header("Shoot Sound")]
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private float clipStartTime = 1f; // seconds into the clip to start playing
    [SerializeField] private float clipEndTime = 2f;   // seconds into the clip to stop playing
    [SerializeField] private int audioSourcePoolSize = 6; // how many overlapping shots can play at once
    [SerializeField][Range(0f, 1f)] private float shootVolume = 1f;

    // Applied to each spawned bullet - power-ups multiply these instead of
    // touching the bullet prefab's own base values.
    public float damageMultiplier = 1f;
    public float rangeMultiplier = 1f;

    // Read by CharacterVisual so the player faces the same way as the gun.
    public static Vector2 AimDirection { get; private set; } = Vector2.right;

    private float fireTimer = 0f;
    private Camera mainCamera;

    private List<AudioSource> audioSourcePool;
    private int nextAudioSourceIndex = 0;

    void Start()
    {
        mainCamera = Camera.main;
        BuildAudioSourcePool();
    }

    void BuildAudioSourcePool()
    {
        audioSourcePool = new List<AudioSource>();
        for (int i = 0; i < audioSourcePoolSize; i++)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            audioSourcePool.Add(src);
        }
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
        AimDirection = direction;

        transform.position = (Vector2)player.position + direction * orbitRadius;

        // Rotate the gun to face outward (same direction it's orbiting toward).
        // Plain continuous rotation - always tracks the mouse 1:1, no popping
        // or reversed sweeps. (A left/right sprite swap was tried here, but
        // mirroring the art to keep it "right side up" also reverses which
        // way it visually rotates relative to the mouse - felt broken.)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);

        // Past vertical (aiming into the left half), the rotation alone would
        // leave the art upside-down. Flipping the sprite top-to-bottom at
        // that point corrects the "which way is up" reading without touching
        // the rotation value itself, so the mouse-tracking above is unaffected.
        if (visualRenderer != null)
            visualRenderer.flipY = direction.x < 0f;
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

        PlayShootSoundSegment();
    }

    void PlayShootSoundSegment()
    {
        if (shootSound == null || audioSourcePool == null || audioSourcePool.Count == 0) return;

        AudioSource src = audioSourcePool[nextAudioSourceIndex];
        nextAudioSourceIndex = (nextAudioSourceIndex + 1) % audioSourcePool.Count;

        src.Stop(); // in case this pooled source is still finishing an older, overlapping shot
        src.clip = shootSound;
        src.volume = shootVolume;
        src.time = clipStartTime;
        src.Play();

        float segmentDuration = Mathf.Max(0f, clipEndTime - clipStartTime);
        src.SetScheduledEndTime(AudioSettings.dspTime + segmentDuration);
    }

    public void IncreaseFireRate(float percent)
    {
        fireRate /= 1f + percent;
    }

    public void EnableGun()
    {
        enabled = true;
    }
}