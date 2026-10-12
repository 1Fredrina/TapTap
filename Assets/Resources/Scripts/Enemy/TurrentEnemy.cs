using UnityEngine;

public class TurretEnemy : Enemy
{
    [Header("扇形检测设置")]
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private float detectionAngle = 60f;
    [SerializeField] private LayerMask playerLayer;

    [Header("射击设置")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireInterval = 1.5f;

    private float fireTimer;
    private Transform playerTarget;
    private Animator animator;

    protected override void Awake()
    {
        base.Awake();
        animator = GetComponent<Animator>();
    }

    protected override void Update()
    {
        base.Update();
        if (IsDead || IsStunned) return;

        DetectPlayer();

        bool readyToFire = UpdatePlayerVisibility(playerTarget != null);

        if (readyToFire)
        {
            fireTimer -= Time.deltaTime;
            if (fireTimer <= 0f)
            {
                Shoot();
                fireTimer = fireInterval;
            }
        }
        else
        {
            fireTimer = fireInterval;
        }
    }

    private Vector2 GetForwardDirection()
    {
        return -transform.right;
    }

    private void DetectPlayer()
    {
        playerTarget = null;
        Vector2 forward = GetForwardDirection();
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, playerLayer);

        foreach (var hit in hits)
        {
            Vector2 toPlayer = (Vector2)(hit.transform.position - transform.position);
            float angle = Vector2.Angle(forward, toPlayer);
            if (angle <= detectionAngle * 0.5f)
            {
                playerTarget = hit.transform;
                break;
            }
        }
    }

    private void Shoot()
    {
        if (bulletPrefab == null || firePoint == null || playerTarget == null) return;

        if (animator != null)
            animator.SetTrigger("IsAtk");

        Vector2 shootDirection = ((Vector2)playerTarget.position - (Vector2)firePoint.position).normalized;
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

        if (bullet.TryGetComponent<EnemyBullet>(out var bulletScript))
        {
            int dmg = Data != null ? Data.AttackDamage : 1;
            bulletScript.Initialize(shootDirection, dmg);
        }
    }

    protected override void Die()
    {
        base.Die();
        ThrowerEnemy.NotifyTurretDied();
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 forward = GetForwardDirection();
        Vector2 leftBoundary  = Quaternion.Euler(0, 0,  detectionAngle * 0.5f) * forward;
        Vector2 rightBoundary = Quaternion.Euler(0, 0, -detectionAngle * 0.5f) * forward;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)leftBoundary  * detectionRadius);
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)rightBoundary * detectionRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)forward * detectionRadius);
    }
}