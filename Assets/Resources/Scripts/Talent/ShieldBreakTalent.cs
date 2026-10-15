using UnityEngine;

[System.Serializable]
public class ShieldBreakTalent : TalentBase
{
    public float radius = 3.5f;
    public int damage = 20;
    public float stunDuration = 1f;
    public float cooldown = 6f;
    public string effectPrefabPath;

    public override void OnAcquire(Player player, int stack)
    {
        if (player == null) return;
        GameObject effect = string.IsNullOrEmpty(effectPrefabPath) ? null : Resources.Load<GameObject>(effectPrefabPath);
        player.TalentEffects.ConfigureShieldBreak(radius, damage * stack, stunDuration, cooldown, effect);
    }
}
