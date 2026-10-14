using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class OrbitWeapon : WeaponBase
{
    [Min(1)] public int orbitCount = 2;
    [Min(0.5f)] public float orbitRadius = 5f;
    public float orbitSpeed = 120f;
    public string orbitPrefabPath = "Prefabs/Weapons/OrbitProjectile";

    public override bool RequiresTarget => false;
    public override bool RequiresEnemyInRange => false;

    public override void Fire(Player player, Enemy target) { }

    [System.NonSerialized] private readonly List<GameObject> orbitObjects = new();

    public void Refresh(Player player)
    {
        Clear();

        GameObject prefab = Resources.Load<GameObject>(orbitPrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[OrbitWeapon] prefab not found: {orbitPrefabPath}");
            return;
        }

        int count = Mathf.Max(1, orbitCount) + extraShots;
        float dmg = damage * player.DamageMultiplier;

        for (int i = 0; i < count; i++)
        {
            float startAngle = i * (360f / count);
            GameObject obj = Object.Instantiate(prefab, player.transform.position, Quaternion.identity);
            if (obj.TryGetComponent<OrbitProjectile>(out var op))
            {
                op.Init(player, dmg, orbitRadius, orbitSpeed, startAngle);
            }
            orbitObjects.Add(obj);
        }
    }

    public void Clear()
    {
        foreach (var obj in orbitObjects)
        {
            if (obj != null) Object.Destroy(obj);
        }
        orbitObjects.Clear();
    }

    public override void OnStackUp(int newStack)
    {
        base.OnStackUp(newStack);
    }

    protected override void FireSingle(Player player, Enemy target)
    {
        throw new System.NotImplementedException();
    }
}