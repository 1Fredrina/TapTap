using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ChaserEnemy : Enemy
{
    [Header("检测")]
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private LayerMask playerLayer;

    [Header("移动")]
    [SerializeField] private float moveSpeed = 4f;

    private Rigidbody2D rb;
    private Animator animator;
    private Transform playerTarget;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.constraints |= RigidbodyConstraints2D.FreezeRotation;
            rb.gravityScale = 1f;
        }
    }

    protected override void Update()
    {
        base.Update();
        if (IsDead || IsStunned)
        {
            if (animator != null) animator.SetBool("IsRun", false);
            return;
        }

        DetectPlayer();

        if (animator != null)
            animator.SetBool("IsRun", playerTarget != null);
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        if (IsDead || IsStunned || playerTarget == null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        float dirX = playerTarget.position.x - transform.position.x;
        float moveX = Mathf.Sign(dirX) * moveSpeed * MoveSpeedMultiplier;

        rb.linearVelocity = new Vector2(moveX, rb.linearVelocity.y);

        if (Mathf.Abs(dirX) > 0.05f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * -Mathf.Sign(dirX);
            transform.localScale = scale;
        }
    }

    private void DetectPlayer()
    {
        playerTarget = null;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, playerLayer);

        foreach (var hit in hits)
        {
            playerTarget = hit.transform;
            break;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
