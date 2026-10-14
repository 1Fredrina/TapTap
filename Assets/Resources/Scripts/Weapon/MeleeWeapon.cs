using UnityEngine;

[System.Serializable]
public class MeleeWeapon : WeaponBase
{
    [Range(1f, 360f)] public float arcAngle = 90f;
    public GameObject effectPrefab;
    public float effectLifetime = 0.2f;

    public override bool RequiresTarget => true;

    protected override void FireSingle(Player player, Enemy target)
    {
        if (player == null) return;

        Vector2 origin = player.transform.position;
        Vector2 forward = target != null
            ? ((Vector2)target.transform.position - origin).normalized
            : (player.Controller != null
                ? new Vector2(player.Controller.FacingDirection, 0f)
                : Vector2.right);

        if (effectPrefab != null)
        {
            float angle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
            GameObject fx = Object.Instantiate(effectPrefab, origin, Quaternion.Euler(0, 0, angle));
            Object.Destroy(fx, effectLifetime);
        }

        float halfArc = arcAngle * 0.5f;
        float rangeSqr = range * range;
        float dmg = damage * player.DamageMultiplier;

        Enemy[] enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.IsDead || !enemy.isActiveAndEnabled) continue;

            Vector2 toEnemy = (Vector2)enemy.transform.position - origin;
            if (toEnemy.sqrMagnitude > rangeSqr) continue;
            if (Vector2.Angle(forward, toEnemy) > halfArc) continue;

            enemy.TakeDamage(Mathf.RoundToInt(dmg));
        }

        EnemyBullet[] bullets = Object.FindObjectsByType<EnemyBullet>(FindObjectsSortMode.None);
        foreach (var bullet in bullets)
        {
            if (bullet == null) continue;

            Vector2 toBullet = (Vector2)bullet.transform.position - origin;
            if (toBullet.sqrMagnitude > rangeSqr) continue;
            if (Vector2.Angle(forward, toBullet) > halfArc) continue;

            Object.Destroy(bullet.gameObject);
        }
    }
}