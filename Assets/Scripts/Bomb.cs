using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bomb : MonoBehaviour
{
    private Rigidbody2D rb;

    private float bounceRetain;
    private int maxBounces;

    private int bounceCount = 0;
    private bool stopped = false;

    [SerializeField] private float despawnDelay = 1.5f;
    [SerializeField] private float maxLifetime = 10f; 

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 1f;
        rb.freezeRotation = true;


        Destroy(gameObject, maxLifetime);
    }

    public void SetGravityDirection(bool reversed)
    {
        rb.gravityScale = reversed ? -1f : 1f;
    }


    public void Initialize(float speed, float angleDeg, float bounceRetain, int maxBounces)
    {
        this.maxBounces = maxBounces;
        this.bounceRetain = bounceRetain;

        float rad = angleDeg * Mathf.Deg2Rad;


        Vector2 velocity = new Vector2(Mathf.Cos(rad) * speed, Mathf.Sin(rad) * speed);
        rb.velocity = velocity;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (stopped) return;
        if (collision.collider.CompareTag("Player")) return;

        bounceCount++;

        if (bounceCount >= maxBounces)
            StopBomb();
    }

    private void StopBomb()
    {
        stopped = true;
        rb.velocity = Vector2.zero;
        Destroy(gameObject, despawnDelay);
    }
}