using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyVolant : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float maxSpeed = 3f;
    [SerializeField] private float maxAcceleration = 10f;
    [SerializeField] private float stopDelay = 1f;
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;

    [Header("Bomb Interaction")]
    [SerializeField] private float recoilForce = 5f;
    [SerializeField] private float stunDuration = 1f;

    

    private Rigidbody2D _rigidbody;
    private Transform _currentTarget;
    private bool _isWaiting;
    private bool isStunned = false;

    private float _patrolY; 

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _rigidbody.gravityScale = 0;
        _rigidbody.freezeRotation = true;

   
        _patrolY = transform.position.y;


        if (pointA == null || pointB == null)
        {
            pointA = new GameObject("PointA").transform;
            pointB = new GameObject("PointB").transform;

            Vector3 startPos = transform.position;
            pointA.position = startPos + new Vector3(-5f, 0f, 0f);
            pointB.position = startPos + new Vector3(5f, 0f, 0f);
        }

        _currentTarget = pointB;
    }

    private void FixedUpdate()
    {

        transform.position = new Vector3(transform.position.x, _patrolY, transform.position.z);

        if (!_isWaiting && !isStunned)
            HandleMovement();
    }

    private void HandleMovement()
    {
        Vector2 direction = (_currentTarget.position - transform.position).normalized;
        Vector2 targetVelocity = direction * maxSpeed;

        Vector2 velocityDelta = targetVelocity - _rigidbody.velocity;
        velocityDelta = Vector2.ClampMagnitude(velocityDelta, maxAcceleration * Time.fixedDeltaTime);

        _rigidbody.AddForce(velocityDelta * _rigidbody.mass, ForceMode2D.Impulse);

        float distance = Vector2.Distance(transform.position, _currentTarget.position);

        if (distance < 0.2f || Vector2.Dot((_currentTarget.position - transform.position), _rigidbody.velocity) < 0)
        {
            StartCoroutine(SwitchTargetAfterDelay());
        }
    }

    private IEnumerator SwitchTargetAfterDelay()
    {
        _isWaiting = true;
        _rigidbody.velocity = Vector2.zero;
        yield return new WaitForSeconds(stopDelay);
        _currentTarget = (_currentTarget == pointA) ? pointB : pointA;
        _isWaiting = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isStunned) return;

        Bomb bomb = collision.GetComponent<Bomb>();
        if (bomb != null)
        {
            Destroy(bomb.gameObject);


            Vector2 recoilDir = new Vector2(transform.position.x - bomb.transform.position.x, 0).normalized;


            _rigidbody.velocity = recoilDir * recoilForce;

            StartCoroutine(StunAfterHit());
        }
    }

    private IEnumerator StunAfterHit()
    {
        isStunned = true;


        yield return new WaitForSeconds(stunDuration);

        isStunned = false;


        _currentTarget = (_currentTarget == pointA) ? pointB : pointA;
    }
}