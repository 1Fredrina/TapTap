using UnityEngine;

[System.Serializable]
public class KillStackTalent : TalentBase
{
    public float initialDamageMultiplier = 0.5f;
    public float damagePerKill = 0.01f;

    public override void OnAcquire(Player player, int stack)
    {
        if (player == null) return;
        player.SetBaseDamageMultiplier(initialDamageMultiplier);
        player.AddKillStackBonus(damagePerKill);
    }
}