using UnityEngine;

[System.Serializable]
public class WeaponTalent : TalentBase
{
    public WeaponType weaponType;
    public StackUpMode stackUpMode = StackUpMode.ExtraShot;

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

    // Rocket
    public string rocketPrefabPath;
    public string explosionEffectPath;
    public float explosionRadius = 2f;
    public float rocketSpeed = 8f;

    // FireBomb
    public string fireBombPrefabPath;
    public string firePitPrefabPath;
    public float firePitRadius = 2f;
    public float firePitLifetime = 3f;
    public float firePitDamageInterval = 0.5f;
    public float fireBombSpeed = 8f;

    // Piercing
    public string piercingBulletPrefabPath;
    public int basePierce = 1;
    public float piercingBulletSpeed = 12f;

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
                    stackUpMode = stackUpMode,
                    projectilePrefab = LoadPrefab(projectilePrefabPath),
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
                    stackUpMode = stackUpMode,
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
                    stackUpMode = stackUpMode,
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
                    stackUpMode = stackUpMode,
                    orbitCount = orbitCount,
                    orbitRadius = orbitRadius,
                    orbitSpeed = orbitSpeed,
                    orbitPrefabPath = string.IsNullOrEmpty(orbitPrefabPath)
                        ? "Prefabs/Weapons/OrbitProjectile"
                        : orbitPrefabPath
                };

            case WeaponType.Rocket:
                return new RocketWeapon
                {
                    id = id,
                    displayName = displayName,
                    cooldown = cooldown,
                    damage = damage,
                    range = range,
                    stackUpMode = stackUpMode,
                    rocketPrefab = LoadPrefab(rocketPrefabPath),
                    explosionRadius = explosionRadius,
                    rocketSpeed = rocketSpeed,
                    explosionEffectPrefab = LoadPrefab(explosionEffectPath)
                };

            case WeaponType.FireBomb:
                return new FireBombWeapon
                {
                    id = id,
                    displayName = displayName,
                    cooldown = cooldown,
                    damage = damage,
                    range = range,
                    stackUpMode = stackUpMode,
                    bombPrefab = LoadPrefab(fireBombPrefabPath),
                    firePitRadius = firePitRadius,
                    firePitLifetime = firePitLifetime,
                    firePitDamageInterval = firePitDamageInterval,
                    bombSpeed = fireBombSpeed
                };

            case WeaponType.Piercing:
                return new PiercingWeapon
                {
                    id = id,
                    displayName = displayName,
                    cooldown = cooldown,
                    damage = damage,
                    range = range,
                    stackUpMode = stackUpMode,
                    bulletPrefab = LoadPrefab(piercingBulletPrefabPath),
                    basePierce = basePierce,
                    bulletSpeed = piercingBulletSpeed
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
    Orbit,
    Rocket,
    FireBomb,
    Piercing,

}