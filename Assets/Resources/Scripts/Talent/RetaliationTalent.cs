using UnityEngine;

[System.Serializable]
public class RetaliationTalent : TalentBase
{
    public int projectileCount = 8;
    public int damage = 12;
    public float cooldown = 4f;
    public string projectilePrefabPath;

    public override void OnAcquire(Player player, int stack)
    {
        if (player == null) return;
        GameObject prefab = string.IsNullOrEmpty(projectilePrefabPath)
            ? null : Resources.Load<GameObject>(projectilePrefabPath);
        player.SetRetaliation(projectileCount, damage * stack, cooldown, prefab);
    }
}
