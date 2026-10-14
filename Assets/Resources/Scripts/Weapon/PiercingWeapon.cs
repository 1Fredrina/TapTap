using UnityEngine;

[System.Serializable]
public class PiercingWeapon : WeaponBase
{
    public GameObject bulletPrefab;
    public int basePierce = 1;
    public float bulletSpeed = 12f;

    public override bool RequiresTarget => true;

    protected override void FireSingle(Player player, Enemy target)
{
    if (player == null || bulletPrefab == null) return;

    Vector2 direction;
    if (target != null && !target.IsDead)
        direction = ((Vector2)target.transform.position - (Vector2)player.transform.position).normalized;
    else
        direction = player.Controller != null
            ? new Vector2(player.Controller.FacingDirection, 0f)
            : Vector2.right;

    if (direction.sqrMagnitude < 0.001f)
        direction = Vector2.right;

    Vector3 spawnPos = player.transform.position + (Vector3)(direction * 0.5f);
    GameObject bullet = Object.Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

    if (bullet.TryGetComponent<PiercingBullet>(out var pb))
    {
        int pierce = basePierce + extraPierce;
        pb.Init(direction, damage * player.DamageMultiplier, pierce, player.BulletSpeedMultiplier);
    }
}
}