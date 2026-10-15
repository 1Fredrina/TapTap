using UnityEngine;

[System.Serializable]
public class DeathNovaTalent : TalentBase
{
    public int killsRequired = 5;
    public float radius = 2.5f;
    public int damage = 18;
    public string effectPrefabPath;

    public override void OnAcquire(Player player, int stack)
    {
        if (player == null) return;
        GameObject effect = string.IsNullOrEmpty(effectPrefabPath) ? null : Resources.Load<GameObject>(effectPrefabPath);
        player.TalentEffects.ConfigureDeathNova(killsRequired, radius, damage * stack, effect);
    }
}
