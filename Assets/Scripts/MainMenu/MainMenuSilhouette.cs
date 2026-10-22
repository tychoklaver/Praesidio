using UnityEngine;

public class MainMenuSilhouette : MonoBehaviour
{
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] float walkSpeed = 1.4766f;
    [SerializeField] bool walkRight = true;
    [SerializeField] float minX = -11f;
    [SerializeField] float maxX = 11f;
    [SerializeField] LayerMask groundLayer = ~0;

    void Start()
    {
        spriteRenderer.flipX = !walkRight;
    }

    void Update()
    {
        Walk();
        SnapToGround();
    }

    void Walk()
    {
        float direction = walkRight ? 1f : -1f;

        transform.position += Vector3.right * direction * walkSpeed * Time.deltaTime;

        if (walkRight && transform.position.x > maxX)
            transform.position = new Vector3(minX, transform.position.y, transform.position.z);

        if (!walkRight && transform.position.x < minX)
            transform.position = new Vector3(maxX, transform.position.y, transform.position.z);
    }

    void SnapToGround()
    {
        Vector2 origin = new Vector2(transform.position.x, transform.position.y + 3f);

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, 10f, groundLayer);

        if (hit.collider != null)
            transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
    }
}
