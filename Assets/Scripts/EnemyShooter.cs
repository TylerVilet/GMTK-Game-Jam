using System.Collections.Generic;
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

    [Header("Shoot Sound")]
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private float clipStartTime = 1f; // seconds into the clip to start playing
    [SerializeField] private float clipEndTime = 2f;   // seconds into the clip to stop playing
    [SerializeField] private int audioSourcePoolSize = 4; // how many overlapping shots can play at once
    [SerializeField][Range(0f, 1f)] private float shootVolume = 1f;

    public float damageMultiplier = 1f;
    public float rangeMultiplier = 1f;

    private float fireTimer = 0f;
    private Transform player;

    private List<AudioSource> audioSourcePool;
    private int nextAudioSourceIndex = 0;

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;

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

        transform.position = (Vector2)enemy.position + direction * orbitRadius;

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

        PlayShootSoundSegment();
    }

    void PlayShootSoundSegment()
    {
        if (shootSound == null || audioSourcePool == null || audioSourcePool.Count == 0) return;

        AudioSource src = audioSourcePool[nextAudioSourceIndex];
        nextAudioSourceIndex = (nextAudioSourceIndex + 1) % audioSourcePool.Count;

        src.Stop();
        src.clip = shootSound;
        src.volume = shootVolume;
        src.time = clipStartTime;
        src.Play();

        float segmentDuration = Mathf.Max(0f, clipEndTime - clipStartTime);
        src.SetScheduledEndTime(AudioSettings.dspTime + segmentDuration);
    }
}