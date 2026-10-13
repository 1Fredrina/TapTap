using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BerserkerEnemy : Enemy
{
    public enum AttackState { Idle, Shooting, Charging, Slamming, Cooling }

    [Header("检测")]
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private LayerMask playerLayer;

    [Header("移动")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float preferredDistance = 6f;
    [SerializeField] private float distanceTolerance = 0.5f;

    [Header("攻击间隔")]
    [SerializeField] private float attackCooldown = 2f;

    [Header("射击")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float shootInterval = 1.5f;
    [SerializeField] private int shootCount = 2;
    [SerializeField] private float shootSpreadAngle = 15f;

    [Header("冲锋")]
    [SerializeField] private float chargeForce = 10f;
    [SerializeField] private float chargeDuration = 0.6f;

    [Header("砸地")]
    [SerializeField] private float slamJumpForce = 12f;
    [SerializeField] private GameObject shockwavePrefab;
    [SerializeField] private Vector2 shockwaveOffset = new Vector2(0.8f, 0f);
    [SerializeField] private float shockwaveYOffset = -0.5f;

    [Header("地面检测")]
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private float groundCheckDistance = 0.15f;

    [Header("半血狂暴")]
    [SerializeField] private float berserkThreshold = 0.5f;

    private bool IsBerserk
    {
        get
        {
            if (Data == null) return false;
            return CurrentHealth / Data.MaxHealth <= berserkThreshold;
        }
    }

    private Rigidbody2D rb;
    private Animator animator;
    private Transform playerTarget;

    private AttackState state = AttackState.Idle;
    private float stateTimer;
    private bool hasLanded;

    private int remainingAttacks = 0;
    private AttackState queuedAttackType;

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
            if (rb != null)
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        DetectPlayer();
        UpdateMovement();

        switch (state)
        {
            case AttackState.Idle:     UpdateIdle();     break;
            case AttackState.Shooting: UpdateShooting(); break;
            case AttackState.Charging: UpdateCharging(); break;
            case AttackState.Slamming: UpdateSlamming(); break;
            case AttackState.Cooling:  UpdateCooling();  break;
        }
    }

    private void UpdateMovement()
    {
        if (rb == null || playerTarget == null) return;

        if (state != AttackState.Idle && state != AttackState.Cooling) return;

        float dirX = playerTarget.position.x - transform.position.x;
        float distance = Mathf.Abs(dirX);

        if (Mathf.Abs(distance - preferredDistance) <= distanceTolerance)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        float moveDir = distance > preferredDistance ? Mathf.Sign(dirX) : -Mathf.Sign(dirX);
        rb.linearVelocity = new Vector2(moveDir * moveSpeed, rb.linearVelocity.y);

        if (Mathf.Abs(dirX) > 0.05f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(dirX);
            transform.localScale = scale;
        }
    }
    private void UpdateIdle()
    {
        if (playerTarget == null) return;

        int choice = Random.Range(0, 3);
        AttackState chosen;
        switch (choice)
        {
            case 0: chosen = AttackState.Shooting; break;
            case 1: chosen = AttackState.Charging; break;
            default: chosen = AttackState.Slamming; break;
        }

        remainingAttacks = IsBerserk ? 2 : 1;
        queuedAttackType = chosen;

        StartAttack(chosen);
    }

    private void StartAttack(AttackState attack)
    {
        switch (attack)
        {
            case AttackState.Shooting: StartShooting(); break;
            case AttackState.Charging: StartCharging(); break;
            case AttackState.Slamming: StartSlamming(); break;
        }
    }

    private void UpdateShooting()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
            FinishOneAttack();
    }

    private void UpdateCharging()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            FinishOneAttack();
        }
    }

    private void UpdateSlamming()
    {
        if (!hasLanded)
        {
            if (stateTimer > 0f)
            {
                stateTimer -= Time.deltaTime;
                return;
            }

            if (IsGrounded())
            {
                hasLanded = true;
                OnSlamLand();
            }
            return;
        }

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
            FinishOneAttack();
    }

    private void UpdateCooling()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
            state = AttackState.Idle;
    }

    private void FinishOneAttack()
    {
        remainingAttacks--;

        if (remainingAttacks > 0)
        {
            StartAttack(queuedAttackType);
        }
        else
        {
            EnterCooling();
        }
    }
    private void StartShooting()
    {
        state = AttackState.Shooting;
        stateTimer = 0.5f;
        if (animator != null) animator.SetTrigger("IsShoot");

        ShootBullets(shootCount);
    }

    private void StartCharging()
    {
        state = AttackState.Charging;
        stateTimer = chargeDuration;

        ApplyChargeForce();
    }

    private void ApplyChargeForce()
    {
        if (playerTarget == null || rb == null) return;

        float dirX = playerTarget.position.x - transform.position.x;
        float facing = dirX >= 0f ? 1f : -1f;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * facing;
        transform.localScale = scale;

        rb.linearVelocity = new Vector2(facing * chargeForce, rb.linearVelocity.y);

        if (animator != null) animator.SetTrigger("IsCharge");
    }

    private void StartSlamming()
    {
        state = AttackState.Slamming;
        hasLanded = false;
        stateTimer = 0.2f;

        rb.linearVelocity = new Vector2(0f, slamJumpForce * 0.4f);

        if (animator != null) animator.SetTrigger("IsSlam");
    }

    private void OnSlamLand()
    {
        if (shockwavePrefab != null)
            SpawnShockwavePair();

        stateTimer = 0.4f;
        if (animator != null) animator.SetTrigger("IsLand");
    }

    private void SpawnShockwavePair()
    {
        float offsetX = shockwaveOffset.x;
        Vector2 basePos = (Vector2)transform.position + new Vector2(0f, shockwaveYOffset);

        GameObject leftWave = Instantiate(
            shockwavePrefab,
            basePos + new Vector2(-offsetX, shockwaveOffset.y),
            Quaternion.identity);
        leftWave.GetComponent<Shockwave>()?.SetDirection(-1f);

        GameObject rightWave = Instantiate(
            shockwavePrefab,
            basePos + new Vector2(offsetX, shockwaveOffset.y),
            Quaternion.identity);
        rightWave.GetComponent<Shockwave>()?.SetDirection(1f);
    }

    private void EnterCooling()
    {
        state = AttackState.Cooling;
        stateTimer = attackCooldown;
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

    private bool IsGrounded()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return false;

        Vector2 origin = new Vector2(col.bounds.center.x, col.bounds.min.y);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayers);
        return hit.collider != null;
    }

    private void ShootBullets(int count)
    {
        if (bulletPrefab == null || firePoint == null || playerTarget == null) return;

        Vector2 baseDir = ((Vector2)playerTarget.position - (Vector2)firePoint.position).normalized;
        int dmg = Data != null ? Data.AttackDamage : 1;

        for (int i = 0; i < count; i++)
        {
            float offset = (i - (count - 1) / 2f) * shootSpreadAngle;
            Vector2 dir = Quaternion.Euler(0, 0, offset) * baseDir;

            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
            if (bullet.TryGetComponent<EnemyBullet>(out var bulletScript))
                bulletScript.Initialize(dir, dmg);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        if (shockwavePrefab != null)
        {
            Vector2 basePos = (Vector2)transform.position + new Vector2(0f, shockwaveYOffset);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(basePos + new Vector2(-shockwaveOffset.x, shockwaveOffset.y), 0.3f);
            Gizmos.DrawWireSphere(basePos + new Vector2(shockwaveOffset.x, shockwaveOffset.y), 0.3f);
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Vector2 origin = new Vector2(col.bounds.center.x, col.bounds.min.y);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(origin, origin + Vector2.down * groundCheckDistance);
        }
    }
}