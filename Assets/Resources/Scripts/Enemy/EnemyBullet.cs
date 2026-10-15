using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private int damage = 10;

    [Header("碰撞过滤")]
    [SerializeField] private LayerMask hitLayers;

    private Vector2 moveDirection;

    public void Initialize(Vector2 direction, int bulletDamage=-1)
    {
        moveDirection = direction.normalized;
        if (bulletDamage >= 0)
            damage = bulletDamage;

        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.Translate(moveDirection * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((hitLayers.value & (1 << other.gameObject.layer)) == 0) return;

        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            player.TakeDamage(Enemy.GetUpgradedDamage(damage));
        }
        Destroy(gameObject);
    }
}
