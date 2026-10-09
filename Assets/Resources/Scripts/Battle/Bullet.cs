using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private int baseDamage = 1;

    private float direction = 1f;

    public void Init(float dir, float damageMultiplier = 1f)
    {
        direction = dir;

        int finalDamage = Mathf.RoundToInt(baseDamage * damageMultiplier);

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * dir;
        transform.localScale = scale;

        currentDamage = finalDamage;

        Destroy(gameObject, lifetime);
    }

    private int currentDamage = 1;

    private void Update()
    {
        transform.Translate(Vector3.right * direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 命中敌人
        //if (other.TryGetComponent<Enemy>(out var enemy))
        //{
        //    enemy.TakeDamage(currentDamage);
        //    Destroy(gameObject);
        //}
    }
}