using UnityEngine;

public abstract class SkillBase
{
    public string SkillName { get; protected set; }
    public float Cooldown { get; set; }

    protected SkillBase(string name, float cooldown)
    {
        SkillName = name;
        Cooldown = cooldown;
    }

    public abstract void Cast(Player caster);
}