using UnityEngine;

[System.Serializable]
public class DashBuffTalent : TalentBase
{
    public float duration = 3f;
    public float damageBonus = 0.5f;

    public override void OnAcquire(Player player, int stack)
    {
        if (player == null) return;
        player.SetDashBuff(duration, damageBonus);
    }
}