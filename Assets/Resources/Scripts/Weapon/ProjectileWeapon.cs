using UnityEngine;

[System.Serializable]
public class ProjectileWeapon : WeaponBase
{
    public GameObject projectilePrefab;
    [Min(1)] public int projectileCount = 1;
    [Range(0f, 180f)] public float projectileSpread = 12f;

    public ProjectileWeapon()
    {
        id = "projectile";
        displayName = "弹幕";
    }

    public override void Fire(Player player, Enemy target)
    {
        if (target == null || projectilePrefab == null) return;

        Vector2 baseDirection = ((Vector2)target.transform.position - (Vector2)player.transform.position).normalized;
        if (baseDirection.sqrMagnitude < 0.001f)
            baseDirection = player.Controller != null
                ? new Vector2(player.Controller.FacingDirection, 0f)
                : Vector2.right;

        int count = Mathf.Max(1, projectileCount);
        for (int i = 0; i < count; i++)
        {
            float angle = count == 1 ? 0f : (i - (count - 1) / 2f) * projectileSpread;
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * baseDirection;

            Vector3 spawnPos = player.transform.position + (Vector3)(direction.normalized * 0.5f);
            GameObject projectile = GameObject.Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            if (projectile.TryGetComponent<Bullet>(out var bullet))
            {
                bullet.Init(direction, damage * player.DamageMultiplier);
            }
        }
    }
}