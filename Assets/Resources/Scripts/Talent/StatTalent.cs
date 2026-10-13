using UnityEngine;

[System.Serializable]
public class StatTalent : TalentBase
{
    public StatType statType;
    public float value;

    public override void OnAcquire(Player player, int stack)
    {
        if (player == null) return;
        player.ApplyStatTalent(statType, value, stack);
    }
}

public enum StatType
{
    AttackDamage,
    AttackSpeed,
    MoveSpeed,
    MaxHealth,
    PickupRange,
    ExperienceGain,
    InvincibleTime,
    SkillCooldown
}