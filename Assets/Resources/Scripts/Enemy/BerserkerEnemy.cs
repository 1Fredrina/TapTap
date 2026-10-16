using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BerserkerEnemy : Enemy
{
    public enum AttackState { Idle, Shooting, Charging, Cooling }

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

    [Header("半血狂暴")]
    [SerializeField] private float berserkThreshold = 0.5f;

    private bool IsBerserk
    {
        get
        {
            if (Data == null) return false;
            return CurrentHealth / MaxHealth <= berserkThreshold;
        }
    }

    private Rigidbody2D rb;
    private Animator animator;
    private Transform playerTarget;

    private AttackState state = AttackState.Idle;
    private float stateTimer;

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
        rb.linearVelocity = new Vector2(moveDir * moveSpeed * MoveSpeedMultiplier, rb.linearVelocity.y);

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

        int choice = Random.Range(0, 2);
        AttackState chosen;
        switch (choice)
        {
            case 0: chosen = AttackState.Shooting; break;
            default: chosen = AttackState.Charging; break;
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

        rb.linearVelocity = new Vector2(facing * chargeForce * MoveSpeedMultiplier, rb.linearVelocity.y);

        if (animator != null) animator.SetTrigger("IsCharge");
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
    }
}