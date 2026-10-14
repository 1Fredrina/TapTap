using UnityEngine;

[System.Serializable]
public class WeaponTalent : TalentBase
{
    public WeaponType weaponType;

    [Min(0.05f)] public float cooldown = 1f;
    [Min(0)] public int damage = 1;
    [Min(0f)] public float range = 12f;

    // Projectile
    public int projectileCount = 1;
    public float projectileSpread = 12f;
    public string projectilePrefabPath;

    // Melee
    public float arcAngle = 90f;
    public string effectPrefabPath;
    public float effectLifetime = 0.2f;

    // Aura
    public string auraPrefabPath;

    // Orbit
    public int orbitCount = 2;
    public float orbitRadius = 5f;
    public float orbitSpeed = 120f;
    public string orbitPrefabPath = "Prefabs/Weapons/OrbitProjectile";

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
                GameObject prefab = LoadPrefab(projectilePrefabPath);
                return new ProjectileWeapon
                {
                    id = id,
                    displayName = displayName,
                    cooldown = cooldown,
                    damage = damage,
                    range = range,
                    projectilePrefab = prefab,
                    projectileCount = projectileCount,
                    projectileSpread = projectileSpread
                };

            case WeaponType.Melee:
                return new MeleeWeapon
                {
                    id = id,
                    displayName = displayName,
                    cooldown = cooldown,
                    damage = damage,
                    range = range,
                    arcAngle = arcAngle,
                    effectPrefab = LoadPrefab(effectPrefabPath),
                    effectLifetime = effectLifetime
                };

            case WeaponType.Aura:
                return new AuraWeapon
                {
                    id = id,
                    displayName = displayName,
                    cooldown = cooldown,
                    damage = damage,
                    range = range,
                    auraPrefab = LoadPrefab(auraPrefabPath)
                };

            case WeaponType.Orbit:
                return new OrbitWeapon
                {
                    id = id,
                    displayName = displayName,
                    cooldown = cooldown,
                    damage = damage,
                    range = range,
                    orbitCount = orbitCount,
                    orbitRadius = orbitRadius,
                    orbitSpeed = orbitSpeed,
                    orbitPrefabPath = string.IsNullOrEmpty(orbitPrefabPath)
                        ? "Prefabs/Weapons/OrbitProjectile"
                        : orbitPrefabPath
                };

            default:
                return null;
        }
    }

    private GameObject LoadPrefab(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        return Resources.Load<GameObject>(path);
    }
}

public enum WeaponType
{
    Projectile,
    Melee,
    Aura,
    Orbit
}