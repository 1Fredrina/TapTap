using UnityEngine;

[System.Serializable]
public class DashPulseTalent : TalentBase
{
    public float radius = 3f;
    public int damage = 10;
    public float stunDuration = 0.6f;
    public float cooldown = 2f;
    public string effectPrefabPath;

    public override void OnAcquire(Player player, int stack)
    {
        if (player == null) return;
        GameObject effect = string.IsNullOrEmpty(effectPrefabPath)
            ? null : Resources.Load<GameObject>(effectPrefabPath);
        player.SetDashPulse(radius, damage * stack, stunDuration, cooldown, effect);
    }
}
