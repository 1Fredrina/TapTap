using UnityEngine;

public class PiercingBullet : MonoBehaviour
{
    public float speed = 12f;
    public float damage;
    public int pierceRemaining;
    public float lifeTime = 3f;

    private Vector2 direction;
    private float lifeTimer;

    public void Init(Vector2 direction, float damage, int pierce, float speedMultiplier = 1f)
    {
        this.direction = direction.normalized;
        this.damage = damage;
        this.pierceRemaining = pierce;
        speed *= Mathf.Max(0.1f, speedMultiplier);
        lifeTimer = lifeTime;

        float angle = Mathf.Atan2(this.direction.y, this.direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null || enemy.IsDead) return;

        enemy.TakeDamage(Mathf.RoundToInt(damage));

        pierceRemaining--;
        if (pierceRemaining <= 0)
            Destroy(gameObject);
    }
}