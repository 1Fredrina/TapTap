using UnityEngine;

[System.Serializable]
public class LastStandTalent : TalentBase
{
    public float cooldown = 45f;
    public float shieldPercent = 0.35f;
    public float invincibility = 2f;
    public string effectPrefabPath;

    public override void OnAcquire(Player player, int stack)
    {
        if (player == null) return;
        GameObject effect = string.IsNullOrEmpty(effectPrefabPath) ? null : Resources.Load<GameObject>(effectPrefabPath);
        player.TalentEffects.ConfigureLastStand(cooldown, shieldPercent, invincibility, effect);
    }
}
