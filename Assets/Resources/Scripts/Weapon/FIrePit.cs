using UnityEngine;

public class FirePit : MonoBehaviour
{
    public float damage;
    public float radius;
    public float lifetime = 3f;
    public float damageInterval = 0.5f;

    private float lifeTimer;
    private float damageTimer;

    public void Init(float damage, float radius, float lifetime, float damageInterval)
    {
        this.damage = damage;
        this.radius = radius;
        this.lifetime = lifetime;
        this.damageInterval = damageInterval;

        lifeTimer = lifetime;
        damageTimer = 0f;

        // 可视化缩放
        transform.localScale = Vector3.one * radius * 2f;
    }

    private void Update()
    {
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        damageTimer -= Time.deltaTime;
        if (damageTimer <= 0f)
        {
            DealDamage();
            damageTimer = damageInterval;
        }
    }

    private void DealDamage()
    {
        Vector2 origin = transform.position;
        float rangeSqr = radius * radius;
        int dmg = Mathf.RoundToInt(damage);

        Enemy[] enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.IsDead || !enemy.isActiveAndEnabled) continue;
            if (((Vector2)enemy.transform.position - origin).sqrMagnitude > rangeSqr) continue;

            enemy.TakeDamage(dmg);
        }
    }
}