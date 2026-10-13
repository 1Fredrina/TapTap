using UnityEngine;

[System.Serializable]
public class WeaponTalent : TalentBase
{
    public WeaponType weaponType;

    [Min(0.05f)] public float cooldown = 1f;
    [Min(0)] public int damage = 1;
    [Min(0f)] public float range = 12f;

    public GameObject projectilePrefab;
    [Min(1)] public int projectileCount = 1;
    [Range(0f, 180f)] public float projectileSpread = 12f;

    public override void OnAcquire(Player player, int stack)
    {
        if (player == null) return;

        WeaponBase weapon = CreateWeapon();
        if (weapon == null) return;

        player.AddOrUpgradeWeapon(weapon, stack);
    }

    private WeaponBase CreateWeapon()
    {
        switch (weaponType)
        {
            case WeaponType.Projectile:
                return new ProjectileWeapon
                {
                    id = id,
                    displayName = displayName,
                    cooldown = cooldown,
                    damage = damage,
                    range = range,
                    projectilePrefab = projectilePrefab,
                    projectileCount = projectileCount,
                    projectileSpread = projectileSpread
                };
            default:
                return null;
        }
    }
}

public enum WeaponType
{
    Projectile,
    Melee,
    Aura
}