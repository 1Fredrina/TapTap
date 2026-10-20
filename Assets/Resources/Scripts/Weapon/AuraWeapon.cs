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

        if (player == null) return;

        if (auraPrefab == null)
        {
            auraInstance = WeaponRangeVisual.Create(player, range, 360f, Vector2.right,
                new Color(1f, 0.35f, 0.08f, 1f));
            return;
        }

        auraInstance = Object.Instantiate(auraPrefab, player.transform.position, Quaternion.identity);
        if (auraInstance.TryGetComponent<AuraVisual>(out var visual))
            visual.Init(player, range);
    }

    public void Clear()
    {
        if (auraInstance != null)
        {
            auraInstance.SetActive(false);
            Object.Destroy(auraInstance);
        }
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
