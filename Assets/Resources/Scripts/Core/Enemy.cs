using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyData data = new EnemyData();

    private float currentHealth;
    private float stunTimer;

    public EnemyData Data => data;
    public float CurrentHealth => currentHealth;
    public bool IsDead { get; private set; }
    public bool IsStunned => stunTimer > 0f;

    private void Awake()
    {
        ResetState();
    }

    private void Update()
    {
        if (stunTimer > 0f)
            stunTimer = Mathf.Max(0f, stunTimer - Time.deltaTime);
    }

    public void Initialize(EnemyData enemyData)
    {
        data = enemyData ?? new EnemyData();
        ResetState();
    }

    /// <summary>
    /// 敌人不使用受击无敌或击退；每次有效命中都会直接扣除生命值。
    /// </summary>
    public void TakeDamage(int damage = 1)
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

    private void ResetState()
    {
        currentHealth = data != null ? Mathf.Max(1f, data.MaxHealth) : 1f;
        stunTimer = 0f;
        IsDead = false;
    }

    private void Die()
    {
        IsDead = true;
        stunTimer = 0f;
        gameObject.SetActive(false);
    }
}
