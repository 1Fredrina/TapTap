using UnityEngine;

[System.Serializable]
public class RocketWeapon : WeaponBase
{
    public GameObject rocketPrefab;
    public float explosionRadius = 2f;
    public float rocketSpeed = 8f;
    public GameObject explosionEffectPrefab;

    public override bool RequiresTarget => true;

    protected override void FireSingle(Player player, Enemy target)
    {
        if (player == null || rocketPrefab == null) return;

        Vector2 direction;
        if (target != null && !target.IsDead)
        {
            direction = ((Vector2)target.transform.position - (Vector2)player.transform.position).normalized;
        }
        else
        {
            direction = player.Controller != null
                ? new Vector2(player.Controller.FacingDirection, 0f)
                : Vector2.right;
        }

        if (direction.sqrMagnitude < 0.001f)
            direction = Vector2.right;

        float shotDamage = damage * player.DamageMultiplier;
        player.SpecialEffects.FireProjectileVolley(() =>
        {
            Vector3 spawnPos = player.transform.position + (Vector3)(direction * 0.5f);
            GameObject rocket = Object.Instantiate(rocketPrefab, spawnPos, Quaternion.identity);
            if (rocket.TryGetComponent<RocketProjectile>(out var rp))
                rp.Init(player, direction, shotDamage, explosionRadius, rocketSpeed, explosionEffectPrefab);
        });
    }
}
