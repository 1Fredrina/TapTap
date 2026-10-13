using UnityEngine;

public class TrapDamage : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifeTime = 2f;

    private bool hasHit = false;

    private void Start()
    {
        if (lifeTime > 0f)
            Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        Player player = other.GetComponentInParent<Player>();
        if (player == null) return;

            player.TakeDamage(damage, transform.position);
        hasHit = true;
        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasHit) return;

        Player player = collision.collider.GetComponentInParent<Player>();
        if (player == null) return;

            player.TakeDamage(damage, transform.position);
        hasHit = true;

    }
}
