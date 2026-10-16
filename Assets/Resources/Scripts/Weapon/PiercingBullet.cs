using UnityEngine;

public class PiercingBullet : MonoBehaviour
{
    public float speed = 12f;
    public float damage;
    public int pierceRemaining;
    public float lifeTime = 3f;
    [SerializeField] private float spriteForwardAngle = 45f;

    private Vector2 direction;
    private float lifeTimer;
    private Player talentOwner;
    private ProjectileTalentEffects talentEffects;
    private bool spent;

    public void SetTalentOwner(Player owner) => talentOwner = owner;

    public void Init(Vector2 direction, float damage, int pierce, float speedMultiplier = 1f)
    {
        this.direction = direction.normalized;
        this.damage = damage;
        this.pierceRemaining = pierce;
        speed *= Mathf.Max(0.1f, speedMultiplier);
        spent = false;
        talentEffects = new ProjectileTalentEffects(talentOwner);
        pierceRemaining += talentEffects.BonusPierce;
        transform.localScale *= talentEffects.SizeMultiplier;
        lifeTimer = lifeTime * talentEffects.LifetimeMultiplier;

        float angle = Mathf.Atan2(this.direction.y, this.direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle - spriteForwardAngle);
    }

    private void Update()
    {
        if (spent) return;
        if (talentEffects != null)
            direction = talentEffects.Steer(transform.position, direction, Time.deltaTime);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle - spriteForwardAngle);
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            spent = true;
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (spent) return;
        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (talentEffects == null || !talentEffects.TryHit(enemy, Mathf.RoundToInt(damage), transform.position)) return;

        if (pierceRemaining > 0)
        {
            pierceRemaining--;
            return;
        }
        if (talentEffects.TryRicochet(transform.position, out Vector2 nextDirection))
        {
            direction = nextDirection;
            return;
        }

        spent = true;
        Destroy(gameObject);
    }
}
