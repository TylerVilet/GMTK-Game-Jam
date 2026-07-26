using UnityEngine;

// Swaps between a left-facing and right-facing sprite based on where the
// active weapon is aiming (not movement direction - twin-stick shooter
// convention), and bounces faster while walking vs. idling - a stand-in for
// a real walk-cycle until there's separate leg/torso art to animate frame by frame.
public class CharacterVisual : MonoBehaviour
{
    public Sprite leftSprite;
    public Sprite rightSprite;

    public float idleBobHeight = 0.15f;
    public float idleBobSpeed = 2.5f;
    public float walkBobHeight = 0.2f;
    public float walkBobSpeed = 7f;
    public float speedThreshold = 0.1f;

    SpriteRenderer sr;
    Rigidbody2D parentRb;
    Vector3 startPos;
    float bobTimer;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        parentRb = GetComponentInParent<Rigidbody2D>();
        startPos = transform.localPosition;
    }

    void Update()
    {
        Vector2 velocity = parentRb != null ? parentRb.linearVelocity : Vector2.zero;
        bool isMoving = velocity.sqrMagnitude > speedThreshold * speedThreshold;

        float aimX = Player.AimDirection.x;
        if (aimX > 0.05f && rightSprite != null)
            sr.sprite = rightSprite;
        else if (aimX < -0.05f && leftSprite != null)
            sr.sprite = leftSprite;

        float height = isMoving ? walkBobHeight : idleBobHeight;
        float speed = isMoving ? walkBobSpeed : idleBobSpeed;
        bobTimer += Time.deltaTime * speed;

        float offset = Mathf.Sin(bobTimer) * height;
        transform.localPosition = startPos + new Vector3(0f, offset, 0f);
    }
}
