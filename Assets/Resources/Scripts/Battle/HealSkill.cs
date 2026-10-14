using UnityEngine;

public class HealSkill : SkillBase
{
    private float shieldPercent;

    public HealSkill(float shieldPercent = 0.2f, float cooldown = 20f)
        : base("Heal", cooldown)
    {
        this.shieldPercent = shieldPercent;
    }

    public override void Cast(Player caster)
    {
        if (caster == null) return;

        float amount = caster.MaxHealth * shieldPercent;
        caster.AddShield(amount);
        Debug.Log($"[Skill] +{amount} shield ({shieldPercent * 100}%)");
    }

    public float ShieldPercent => shieldPercent;

    public void AddShieldPercent(float extra)
    {
        shieldPercent += extra;
    }
}