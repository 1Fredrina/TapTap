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

    public EnemyData Data => data;
    public float CurrentHealth => currentHealth;
    public bool IsDead { get; protected set; }
    public bool IsStunned => stunTimer > 0f;

    protected virtual void Awake()
    {
        ResetState();
    }

    protected virtual void Update()
    {
        if (stunTimer > 0f)
            stunTimer = Mathf.Max(0f, stunTimer - Time.deltaTime);

        if (contactDamageTimer > 0f)
            contactDamageTimer -= Time.deltaTime;
    }

    public virtual void Initialize(EnemyData enemyData)
    {
        data = enemyData ?? new EnemyData();
        ResetState();
    }

    public virtual void TakeDamage(int damage = 1)
    {
        if (IsDead || damage <= 0) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        if (currentHealth <= 0f)
            Die();
    }

    public void Stun(float duration)
    {
        if (IsDead || duration <= 0f) return;

        stunTimer = Mathf.Max(stunTimer, duration);
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

        int dmg = data != null ? data.AttackDamage : 1;
        player.TakeDamage(dmg, transform.position.x);
        contactDamageTimer = contactDamageCooldown;
    }

    protected virtual void ResetState()
    {
        currentHealth = data != null ? Mathf.Max(1f, data.MaxHealth) : 1f;
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
        gameObject.SetActive(false);
    }
}