using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private LayerMask hitLayers;

    private Vector2 direction;
    private int damage;
    private int pierceRemaining;
    private float lifeTimer;
    private Player talentOwner;
    private ProjectileTalentEffects talentEffects;
    private bool spent;

    public void SetTalentOwner(Player owner) => talentOwner = owner;

    public void Init(Vector2 dir, int dmg, int pierce = 0, float speedMultiplier = 1f)
    {
        direction = dir.normalized;
        damage = dmg;
        pierceRemaining = pierce;
        spent = false;
        talentEffects = new ProjectileTalentEffects(talentOwner);
        pierceRemaining += talentEffects.BonusPierce;
        transform.localScale *= talentEffects.SizeMultiplier;
        speed *= Mathf.Max(0.1f, speedMultiplier);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        lifeTimer = lifeTime * talentEffects.LifetimeMultiplier;
    }

    public void Init(Vector2 dir, float dmg, int pierce = 0, float speedMultiplier = 1f)
    {
        Init(dir, Mathf.RoundToInt(dmg), pierce, speedMultiplier);
    }

    private void Update()
    {
        if (spent) return;
        if (talentEffects != null)
            direction = talentEffects.Steer(transform.position, direction, Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
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
        if ((hitLayers.value & (1 << other.gameObject.layer)) == 0) return;

        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            if (talentEffects == null || !talentEffects.TryHit(enemy, damage, transform.position)) return;

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
        }

        spent = true;
        Destroy(gameObject);
    }
}
