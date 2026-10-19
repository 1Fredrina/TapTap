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
    AttackRange,
    MoveSpeed,
    MaxHealth,
    InvincibleTime,
    ShieldDecay,
    PickupRange,
     DamageReduction,
    ExperienceGain,
    SkillCooldown,
    HealOnKill,
    ShieldOnKill,
    ExtraChoice,
    DashCooldown,
    ExtraPierce,
    SizeIncrease,
    KillStackDamage,
    BulletSpeed,
    StationaryAttackSpeed,
    HealthRegeneration,
    HealingReceived,
    DodgeChance,
    // 23 为已移除的抗晕属性保留，避免已有序列化枚举值错位。
    DashDistance = 24,
    ProjectileCriticalChance,
    ProjectileLifetime,
    ProjectileSize,
    ProjectileHoming,
    ProjectileRicochet
}
