using UnityEngine;

public class OrbitProjectile : MonoBehaviour
{
    public float damage;
    public float radius;
    public float angleSpeed;

    private float currentAngle;
    private Player owner;
    private float damageCooldown;
    private readonly float damageInterval = 0.5f;

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

        if (damageCooldown > 0f)
            damageCooldown -= Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        EnemyBullet bullet = other.GetComponent<EnemyBullet>();
        if (bullet != null)
        {
            Destroy(bullet.gameObject);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (damageCooldown > 0f) return;

        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null && !enemy.IsDead)
        {
            enemy.TakeDamage(Mathf.RoundToInt(damage));
            damageCooldown = damageInterval;
        }
    }
}