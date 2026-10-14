using UnityEngine;

[System.Serializable]
public class SkillModTalent : TalentBase
{
    public SkillModType modType;
    public float value;

    public override void OnAcquire(Player player, int stack)
    {
        if (player == null) return;

        SkillBase skill = player.Skill;
        if (skill == null) return;

        if (skill is DoubleActionSkill doubleAction)
        {
            switch (modType)
            {
                case SkillModType.Duration:
                    doubleAction.AddDuration(value);
                    break;
                case SkillModType.MoveSpeed:
                    doubleAction.AddMoveSpeedBonus(value);
                    break;
                case SkillModType.WeaponCooldown:
                    doubleAction.ReduceWeaponCooldown(value);
                    break;
            }
        }
        else if (skill is DamageBuffSkill damageBuff)
        {
            switch (modType)
            {
                case SkillModType.Duration:
                    damageBuff.AddDuration(value);
                    break;
                case SkillModType.Multiplier:
                    damageBuff.AddMultiplier(value);
                    break;
            }
        }
        else if (skill is HealSkill heal)
        {
            switch (modType)
            {
                case SkillModType.TempLifePercent:
                    heal.AddShieldPercent(value);
                    break;
            }
        }
        else if (skill is AoeStunSkill aoeStun)
        {
            switch (modType)
            {
                case SkillModType.StunDuration:
                    aoeStun.AddStunDuration(value);
                    break;
            }
        }
    }
}

public enum SkillModType
{
    Duration,
    MoveSpeed,
    WeaponCooldown,
    Multiplier,
    TempLifePercent,
    StunDuration
}