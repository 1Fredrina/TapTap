using UnityEngine;

public abstract class SkillBase
{
    public string SkillName { get; protected set; }
    public float Cooldown { get; set; }

    public string Id { get; protected set; }

    protected SkillBase(string id, float cooldown)
    {
        Id = id;
        Cooldown = cooldown;
    }

    public abstract void Cast(Player caster);
}