using UnityEngine;

[System.Serializable]
public class AuraWeapon : WeaponBase
{
    public GameObject auraPrefab;

    public override bool RequiresTarget => false;
    public override bool RequiresEnemyInRange => true;

    [System.NonSerialized] private GameObject auraInstance;

    public void Refresh(Player player)
    {
        Clear();

        if (auraPrefab == null || player == null) return;

        auraInstance = Object.Instantiate(auraPrefab, player.transform.position, Quaternion.identity);
        if (auraInstance.TryGetComponent<AuraVisual>(out var visual))
            visual.Init(player, range);
    }

    public void Clear()
    {
        if (auraInstance != null)
            Object.Destroy(auraInstance);
        auraInstance = null;
    }

    protected override void FireSingle(Player player, Enemy target)
    {
        if (player == null) return;

        Vector2 origin = player.transform.position;
        float rangeSqr = range * range;
        float dmg = damage * player.DamageMultiplier;

        Enemy[] enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.IsDead || !enemy.isActiveAndEnabled) continue;

            if (((Vector2)enemy.transform.position - origin).sqrMagnitude > rangeSqr) continue;

            enemy.TakeDamageFrom(Mathf.RoundToInt(dmg), origin);
        }
    }
}
