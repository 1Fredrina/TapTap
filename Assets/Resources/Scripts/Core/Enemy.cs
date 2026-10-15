using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] protected EnemyData data = new EnemyData();

    [Header("通用：进入视野后的开火延迟")]
    [SerializeField] protected float fireDelay = 0.5f;

    [Header("通用：接触伤害")]
    [SerializeField] protected float contactDamageCooldown = 1f;

    protected float currentHealth;
    protected float stunTimer;
    protected float playerInRangeTimer;
    protected bool canFire;
    protected float contactDamageTimer;

    [System.NonSerialized] public float healthMultiplier = 1f;

    private Rigidbody2D body;
    private float talentHealthMultiplier = 1f;

    public float MaxHealth => (data != null ? Mathf.Max(1f, data.MaxHealth) : 1f) * healthMultiplier * talentHealthMultiplier;
    public float MoveSpeedMultiplier => GameManager.Instance?.EnemyTalents.MoveSpeedMultiplier ?? 1f;

    // 在实际命中时统一计算，接触伤害、飞行中的子弹和地面陷阱只乘一次。
    public static int GetUpgradedDamage(int baseDamage)
    {
        float multiplier = GameManager.Instance?.EnemyTalents.DamageMultiplier ?? 1f;
        return Mathf.Max(0, Mathf.RoundToInt(baseDamage * multiplier));
    }

    public void RefreshTalentHealth()
    {
        if (IsDead) return;
        float healthRatio = Mathf.Clamp01(currentHealth / MaxHealth);
        talentHealthMultiplier = GameManager.Instance?.EnemyTalents.HealthMultiplier ?? 1f;
        currentHealth = MaxHealth * healthRatio;
    }

    public EnemyData Data => data;
    public float CurrentHealth => currentHealth;
    public bool IsDead { get; protected set; }
    public bool IsStunned => stunTimer > 0f;

    protected virtual void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.gravityScale = 0f;
            body.constraints |= RigidbodyConstraints2D.FreezeRotation;
        }
        ResetState();
    }

    protected virtual void Update()
    {
        if (stunTimer > 0f)
            stunTimer = Mathf.Max(0f, stunTimer - Time.deltaTime);

        if (contactDamageTimer > 0f)
            contactDamageTimer -= Time.deltaTime;

        if (IsDead) return;

        MoveTowardsPlayer();
    }

    protected virtual void MoveTowardsPlayer()
    {
        if (IsStunned) return;
        if (data == null || data.MoveSpeed <= 0f) return;

        Player target = Player.ActivePlayer;
        if (target == null || target.IsDead) return;

        Vector2 direction = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
        if (direction.sqrMagnitude < 0.001f) return;

        if (body != null)
            body.linearVelocity = direction * data.MoveSpeed * MoveSpeedMultiplier;
        else
            transform.position += (Vector3)(direction * data.MoveSpeed * MoveSpeedMultiplier * Time.deltaTime);
    }

    public virtual void Initialize(EnemyData enemyData)
    {
        data = enemyData ?? new EnemyData();
        ResetState();
    }

    public virtual void Initialize(EnemyData enemyData, float healthMultiplier)
    {
        data = enemyData ?? new EnemyData();
        this.healthMultiplier = Mathf.Max(0.01f, healthMultiplier);
        ResetState();
    }

    public virtual void TakeDamage(int damage = 1)
    {
        if (IsDead || damage <= 0) return;

        float damageTaken = GameManager.Instance?.EnemyTalents.DamageTakenMultiplier ?? 1f;
        currentHealth = Mathf.Max(0f, currentHealth - damage * damageTaken);
        if (currentHealth <= 0f)
            Die();
    }

    public void Stun(float duration)
    {
        if (IsDead || duration <= 0f) return;

        stunTimer = Mathf.Max(stunTimer, duration);

        if (body != null)
            body.linearVelocity = Vector2.zero;
    }

    protected bool UpdatePlayerVisibility(bool hasPlayerInSight)
    {
        if (hasPlayerInSight)
        {
            playerInRangeTimer += Time.deltaTime;
            if (playerInRangeTimer >= fireDelay)
                canFire = true;
        }
        else
        {
            playerInRangeTimer = 0f;
            canFire = false;
        }

        return canFire;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryContactDamage(collision.collider);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryContactDamage(other);
    }

    private void TryContactDamage(Collider2D other)
    {
        if (IsDead) return;
        if (contactDamageTimer > 0f) return;

        Player player = other.GetComponent<Player>();
        if (player == null) return;

        int dmg = data != null ? data.AttackDamage : 10;
        player.TakeDamage(GetUpgradedDamage(dmg));
        contactDamageTimer = contactDamageCooldown;
    }

    protected virtual void ResetState()
    {
        talentHealthMultiplier = GameManager.Instance?.EnemyTalents.HealthMultiplier ?? 1f;
        currentHealth = MaxHealth;
        stunTimer = 0f;
        playerInRangeTimer = 0f;
        canFire = false;
        contactDamageTimer = 0f;
        IsDead = false;
    }

    protected virtual void Die()
    {
        IsDead = true;
        stunTimer = 0f;

        if (body != null)
            body.linearVelocity = Vector2.zero;

        Player.NotifyEnemyDefeated(data != null ? data.ExperienceReward : 100, this);
        Destroy(gameObject);
    }
}
