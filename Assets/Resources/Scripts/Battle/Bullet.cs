using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private LayerMask hitLayers;

    private Vector2 direction;
    private int damage;
    private int pierceRemaining;

    public void Init(Vector2 dir, int dmg, int pierce = 0, float speedMultiplier = 1f)
    {
        direction = dir.normalized;
        damage = dmg;
        pierceRemaining = pierce;
        speed *= Mathf.Max(0.1f, speedMultiplier);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        Destroy(gameObject, lifeTime);
    }

    public void Init(Vector2 dir, float dmg, int pierce = 0, float speedMultiplier = 1f)
    {
        Init(dir, Mathf.RoundToInt(dmg), pierce, speedMultiplier);
    }

    private void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((hitLayers.value & (1 << other.gameObject.layer)) == 0) return;

        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null && !enemy.IsDead)
        {
            enemy.TakeDamage(damage);

            if (pierceRemaining > 0)
            {
                pierceRemaining--;
                return;
            }
        }

        Destroy(gameObject);
    }
}