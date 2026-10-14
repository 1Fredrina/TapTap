using UnityEngine;

public class RocketProjectile : MonoBehaviour
{
    public float speed = 8f;
    public float damage;
    public float explosionRadius = 2f;
    public float maxLifeTime = 3f;
    public GameObject explosionEffectPrefab;

    private Vector2 direction;
    private Player owner;
    private float lifeTimer;

    public void Init(Player owner, Vector2 direction, float damage, float explosionRadius, float speed, GameObject explosionEffect)
    {
        this.owner = owner;
        this.direction = direction.normalized;
        this.damage = damage;
        this.explosionRadius = explosionRadius;
        this.speed = speed;
        this.explosionEffectPrefab = explosionEffect;
        this.lifeTimer = maxLifeTime;

        float angle = Mathf.Atan2(this.direction.y, this.direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
            Explode();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null && !enemy.IsDead)
        {
            Explode();
        }
    }

    private void Explode()
    {
        Vector2 origin = transform.position;
        float rangeSqr = explosionRadius * explosionRadius;

        Enemy[] enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.IsDead || !enemy.isActiveAndEnabled) continue;
            if (((Vector2)enemy.transform.position - origin).sqrMagnitude > rangeSqr) continue;

            enemy.TakeDamage(Mathf.RoundToInt(damage));
        }

        if (explosionEffectPrefab != null)
        {
            GameObject fx = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            fx.transform.localScale = Vector3.one * explosionRadius * 2f;
            Destroy(fx, 0.1f);
        }

        Destroy(gameObject);
    }
}