using UnityEngine;

public class DoubleActionSkill : SkillBase
{
    private float duration;
    private float timer;
    private float dashCooldownMultiplier;

    public DoubleActionSkill(float duration = 10f, float cooldown = 15f, float dashCooldownMultiplier = 0.5f)
        : base("DoubleAction", cooldown)
    {
        this.duration = duration;
        this.dashCooldownMultiplier = dashCooldownMultiplier;
    }

    public override void Cast(Player caster)
    {
        timer = duration;
    }

    public void Tick(float dt)
    {
        if (timer > 0f)
            timer -= dt;
    }

    public bool IsActive => timer > 0f;
    public float DashCooldownMultiplier => dashCooldownMultiplier;
}