using UnityEngine;

[System.Serializable]
public class TravelChargeTalent : TalentBase
{
    public float distance = 8f;
    public float range = 12f;
    public int projectileCount = 5;
    public float spread = 15f;
    public int damage = 6;
    public string projectilePrefabPath;

    public override void OnAcquire(Player player, int stack)
    {
        if (player == null) return;
        GameObject prefab = string.IsNullOrEmpty(projectilePrefabPath) ? null : Resources.Load<GameObject>(projectilePrefabPath);
        player.TalentEffects.ConfigureTravel(distance, range, projectileCount, spread, damage * stack, prefab);
    }
}
