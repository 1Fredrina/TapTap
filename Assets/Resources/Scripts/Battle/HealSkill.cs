using UnityEngine;

public class HealSkill : SkillBase
{
    private int tempLifeAmount;

    public HealSkill(int tempLifeAmount = 2) : base("Heal", 8f)
    {
        this.tempLifeAmount = tempLifeAmount;
    }

    public override void Cast(Player caster)
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.AddTempLives(tempLifeAmount);
        Debug.Log($"[Skill] +{tempLifeAmount} temp lives");
    }
}