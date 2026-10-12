using UnityEngine;

public class ThrowerEnemy : Enemy
{
    [Header("扇形检测（行为1）")]
    [SerializeField] private float detectionRadius = 6f;
    [SerializeField] private float detectionAngle = 90f;
    [SerializeField] private LayerMask playerLayer;

    [Header("射击（行为1）")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float shootInterval = 2f;
    [SerializeField] private float spreadAngle = 15f;

    [Header("抛射炮台（行为2）")]
    [SerializeField] private GameObject turretPrefab;
    [SerializeField] private float throwInterval = 6f;
    [SerializeField] private float throwForceX = 2.5f;
    [SerializeField] private float throwForceY = 4f;

    private float shootTimer;
    private float throwTimer;
    private Transform playerTarget;
    private Animator animator;

    private static int aliveTurretCount = 0;

    public static void NotifyTurretSpawned() => aliveTurretCount++;
    public static void NotifyTurretDied()
    {
        aliveTurretCount = Mathf.Max(0, aliveTurretCount - 1);
    }

    protected override void Awake()
    {
        base.Awake();
        animator = GetComponent<Animator>();
    }

    protected override void Update()
{
    base.Update();
    if (IsDead || IsStunned) return;

    throwTimer -= Time.deltaTime;
    if (throwTimer <= 0f && aliveTurretCount <= 0 && turretPrefab != null)
    {
        ThrowTurrets();
        throwTimer = throwInterval;
    }

    DetectPlayer();

    bool readyToFire = UpdatePlayerVisibility(playerTarget != null);

    if (readyToFire)
    {
        shootTimer -= Time.deltaTime;
        if (shootTimer <= 0f)
        {
            ShootTriple();
            shootTimer = shootInterval;
        }
    }
    else
    {
        shootTimer = shootInterval;
    }
}

    private Vector2 GetForwardDirection()
    {
        return transform.right;
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


    private void ShootTriple()
    {
        if (bulletPrefab == null || firePoint == null || playerTarget == null) return;

        if (animator != null)
            animator.SetTrigger("IsAtk");

        Vector2 baseDir = ((Vector2)playerTarget.position - (Vector2)firePoint.position).normalized;
        int dmg = Data != null ? Data.AttackDamage : 1;

        float[] offsets = { -spreadAngle, 0f, spreadAngle };

        foreach (float offset in offsets)
        {
            Vector2 dir = Quaternion.Euler(0, 0, offset) * baseDir;
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

            if (bullet.TryGetComponent<EnemyBullet>(out var bulletScript))
                bulletScript.Initialize(dir, dmg);
        }
    }

    private void ThrowTurrets()
    {
        if (turretPrefab == null) return;

        if (animator != null)
            animator.SetTrigger("IsThrow");

        SpawnTurret(-1f);
        SpawnTurret( 1f);
    }

    private void SpawnTurret(float horizontalDir)
    {
        Vector3 spawnPos = transform.position + new Vector3(horizontalDir * 1.5f, 1f, 0f);
        GameObject turret = Instantiate(turretPrefab, spawnPos, Quaternion.identity);

        NotifyTurretSpawned();

        Rigidbody2D rb = turret.GetComponent<Rigidbody2D>();
        if (rb != null)
    {
        rb.freezeRotation = true;
        rb.constraints |= RigidbodyConstraints2D.FreezeRotation;
        rb.linearVelocity = new Vector2(horizontalDir * throwForceX, throwForceY);
        rb.gravityScale = 1f;
        rb.bodyType = RigidbodyType2D.Dynamic;
    }
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