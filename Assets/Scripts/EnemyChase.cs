using UnityEngine;

public class EnemyChase : MonoBehaviour
{
    public Rigidbody2D rb;
    public float speed = 3f;
    public string playerTag = "Player";

    private Transform player;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
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
        Vector2 targetPosition = rb.position + direction * speed * Time.fixedDeltaTime;
        rb.MovePosition(targetPosition);
    }
}
