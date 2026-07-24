using System.Collections.Generic;
using UnityEngine;

public class EnemyChase : MonoBehaviour
{
    public Rigidbody2D rb;
    public float speed = 3f;
    public string playerTag = "Player";

    private Transform player;
    readonly Dictionary<Collider2D, Vector2> blockadeContactNormals = new Dictionary<Collider2D, Vector2>();

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    void FixedUpdate()
    {
        if (player == null)
        {
            return;
        }

        Vector2 direction = (player.position - transform.position).normalized;
        Vector2 velocity = direction * speed;

        // Chasing straight into a blockade's edge slides the movement along it
        // instead of just pinning the enemy against it.
        foreach (Vector2 normal in blockadeContactNormals.Values)
        {
            float into = Vector2.Dot(velocity, normal);
            if (into < 0f) velocity -= into * normal;
        }

        Vector2 targetPosition = rb.position + velocity * Time.fixedDeltaTime;
        rb.MovePosition(targetPosition);
    }

    void OnCollisionEnter2D(Collision2D collision) => TrackBlockadeContact(collision);
    void OnCollisionStay2D(Collision2D collision) => TrackBlockadeContact(collision);

    void OnCollisionExit2D(Collision2D collision)
    {
        blockadeContactNormals.Remove(collision.collider);
    }

    void TrackBlockadeContact(Collision2D collision)
    {
        if (!collision.gameObject.name.StartsWith("Blockade")) return;

        Vector2 sum = Vector2.zero;
        int count = collision.contactCount;
        for (int i = 0; i < count; i++) sum += collision.GetContact(i).normal;

        if (count > 0) blockadeContactNormals[collision.collider] = (sum / count).normalized;
    }
}
