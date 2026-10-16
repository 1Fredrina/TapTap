using UnityEngine;

[System.Serializable]
public class FireBombWeapon : WeaponBase
{
    public GameObject bombPrefab;
    public float firePitRadius = 2f;
    public float firePitLifetime = 3f;
    public float firePitDamageInterval = 0.5f;
    public float bombSpeed = 8f;

    public override bool RequiresTarget => true;

    protected override void FireSingle(Player player, Enemy target)
    {
        if (player == null || bombPrefab == null) return;

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

        Vector3 spawnPos = player.transform.position + (Vector3)(direction * 0.5f);
        int bombCount = player.SpecialEffects != null ? player.SpecialEffects.ProjectileCountMultiplier : 1;
        for (int i = 0; i < bombCount; i++)
        {
            GameObject bomb = Object.Instantiate(bombPrefab, spawnPos, Quaternion.identity);
            if (bomb.TryGetComponent<FireBombProjectile>(out var fb))
            {
                fb.Init(
                    player,
                    direction,
                    damage * player.DamageMultiplier,
                    firePitRadius,
                    firePitLifetime,
                    firePitDamageInterval,
                    bombSpeed,
                    Resources.Load<GameObject>("Prefabs/Weapons/FirePit")
                );
            }
        }
    }
}
