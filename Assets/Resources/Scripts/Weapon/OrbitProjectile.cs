using UnityEngine;

public class OrbitProjectile : MonoBehaviour
{
    public float damage;
    public float radius;
    public float angleSpeed;

    private float currentAngle;
    private Player owner;

    public void Init(Player owner, float damage, float radius, float angleSpeed, float startAngle)
    {
        this.owner = owner;
        this.damage = damage;
        this.radius = radius;
        this.angleSpeed = angleSpeed;
        currentAngle = startAngle;
    }

    private void Update()
    {
        if (owner == null) return;

        currentAngle += angleSpeed * Time.deltaTime;
        float rad = currentAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * radius;
        transform.position = owner.transform.position + offset;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 清子弹
        EnemyBullet bullet = other.GetComponent<EnemyBullet>();
        if (bullet != null)
        {
            Destroy(bullet.gameObject);
            return;
        }

        // 对敌人造成一次伤害
        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null && !enemy.IsDead)
        {
            // 环绕物持续存在，命中时读取倍率，使升级和天赋加成即时生效。
            float multiplier = owner != null ? owner.DamageMultiplier : 1f;
            enemy.TakeDamage(Mathf.RoundToInt(damage * multiplier));
        }
    }
}
