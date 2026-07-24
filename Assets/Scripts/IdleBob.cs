using UnityEngine;

// Gentle up-down bobbing for a single static sprite - no sprite sheet needed.
public class IdleBob : MonoBehaviour
{
    public float bobHeight = 0.1f;
    public float bobSpeed = 2f;

    Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        float offset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.localPosition = startPos + new Vector3(0f, offset, 0f);
    }
}
