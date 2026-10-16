using UnityEngine;

public class FireBombProjectile : MonoBehaviour
{
    public float speed = 8f;
    public float damage;
    public float firePitRadius = 2f;
    public float firePitLifetime = 3f;
    public float firePitDamageInterval = 0.5f;
    public float maxLifeTime = 3f;
    public GameObject firePitPrefab;

    private Vector2 direction;
    private Player owner;
    private float lifeTimer;

    public void Init(Player owner, Vector2 direction, float damage, float firePitRadius, float firePitLifetime, float firePitDamageInterval, float speed, GameObject firePitPrefab)
    {
        this.owner = owner;
        this.direction = direction.normalized;
        this.damage = damage;
        this.firePitRadius = firePitRadius;
        this.firePitLifetime = firePitLifetime;
        this.firePitDamageInterval = firePitDamageInterval;
        this.speed = speed;
        this.firePitPrefab = firePitPrefab;
        lifeTimer = maxLifeTime;

        float angle = Mathf.Atan2(this.direction.y, this.direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
            Land();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null && !enemy.IsDead)
        {
            Land();
        }
    }

    private void Land()
    {
        if (firePitPrefab != null)
        {
            GameObject pit = Instantiate(firePitPrefab, transform.position, Quaternion.identity);
            if (pit.TryGetComponent<FirePit>(out var fp))
            {
                fp.Init(owner, damage, firePitRadius, firePitLifetime, firePitDamageInterval);
            }
        }

        Destroy(gameObject);
    }
}
