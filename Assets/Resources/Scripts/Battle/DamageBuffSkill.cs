using UnityEngine;

public class DamageBuffSkill : SkillBase
{
    private float multiplier;
    private float duration;
    private float timer;

    public DamageBuffSkill(float multiplier = 1.5f, float duration = 5f, float cooldown = 20f)
        : base("DamageBuff", cooldown)
    {
        this.multiplier = multiplier;
        this.duration = duration;
    }

    public override void Cast(Player caster)
    {
        if (caster == null) return;

        timer = duration;
        Debug.Log($"[Skill] Damage x{multiplier} for {duration}s");
    }

    public void Tick(float dt)
    {
        if (timer > 0f)
            timer -= dt;
    }

    public bool IsActive => timer > 0f;
    public float Multiplier => multiplier;

    public void AddDuration(float extra)
    {
        duration += extra;
    }

    public void AddMultiplier(float extra)
    {
        multiplier += extra;
    }
}