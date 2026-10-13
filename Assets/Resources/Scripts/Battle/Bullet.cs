using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private int baseDamage = 1;

    private Vector2 direction = Vector2.right;

    public void Init(float dir, float damageMultiplier = 1f)
    {
        direction = new Vector2(dir, 0f);

        int finalDamage = Mathf.RoundToInt(baseDamage * damageMultiplier);

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * dir;
        transform.localScale = scale;

        currentDamage = finalDamage;

        Destroy(gameObject, lifetime);
    }

    public void Init(Vector2 dir, float damageMultiplier = 1f)
    {
        direction = dir.sqrMagnitude > 0.001f ? dir.normalized : Vector2.right;
        int finalDamage = Mathf.RoundToInt(baseDamage * damageMultiplier);
        currentDamage = Mathf.Max(1, finalDamage);
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        Destroy(gameObject, lifetime);
    }

    private int currentDamage = 1;

    private void Update()
    {
        transform.Translate((Vector3)direction * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null) return;

        enemy.TakeDamage(currentDamage);
        Destroy(gameObject);
    }
}
