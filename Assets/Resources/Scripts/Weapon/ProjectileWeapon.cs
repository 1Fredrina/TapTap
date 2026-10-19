using UnityEngine;

[System.Serializable]
public class ProjectileWeapon : WeaponBase
{
    public GameObject projectilePrefab;
    [Min(1)] public int projectileCount = 1;
    [Range(0f, 180f)] public float projectileSpread = 12f;

    public override bool RequiresTarget => true;

    protected override void FireSingle(Player player, Enemy target)
    {
        if (player == null || projectilePrefab == null) return;

        Vector2 baseDirection;
        if (target != null && !target.IsDead)
            baseDirection = ((Vector2)target.transform.position - (Vector2)player.transform.position).normalized;
        else
            baseDirection = player.Controller != null
                ? new Vector2(player.Controller.FacingDirection, 0f)
                : Vector2.right;

        if (baseDirection.sqrMagnitude < 0.001f)
            baseDirection = Vector2.right;

        int baseShotCount = Mathf.Max(1, projectileCount);
        int totalPierce = extraPierce + player.ExtraPierce;
        int dmg = Mathf.RoundToInt(damage * player.DamageMultiplier);
        float speedMult = player.BulletSpeedMultiplier;

        player.SpecialEffects.FireProjectileVolley(() =>
        {
            for (int i = 0; i < baseShotCount; i++)
            {
                float angle = baseShotCount == 1
                    ? 0f
                    : (i - (baseShotCount - 1) / 2f) * projectileSpread;

                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * baseDirection;
                Vector3 spawnPos = player.transform.position + (Vector3)(direction.normalized * 0.5f);

                GameObject projectile = Object.Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
                if (projectile.TryGetComponent<Bullet>(out var bullet))
                {
                    bullet.SetTalentOwner(player);
                    bullet.Init(direction, dmg, totalPierce, speedMult);
                }
            }
        });
    }
}
