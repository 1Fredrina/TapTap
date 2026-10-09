using UnityEngine;

public class DamageBuffSkill : SkillBase
{
    private float multiplier;
    private float duration;
    private float timer;

    public DamageBuffSkill(float multiplier = 2f, float duration = 4f, float cooldown = 6f)
        : base("DamageBuff", cooldown)
    {
        this.multiplier = multiplier;
        this.duration = duration;
    }

    public override void Cast(Player caster)
    {
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
}