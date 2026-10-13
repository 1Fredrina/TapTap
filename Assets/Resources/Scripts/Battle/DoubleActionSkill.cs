using UnityEngine;

public class DoubleActionSkill : SkillBase
{
    private float duration;
    private float timer;
    private float moveSpeedBonus;
    private float weaponCooldownMultiplier;

    public DoubleActionSkill(
        float duration = 6f,
        float cooldown = 18f,
        float moveSpeedBonus = 0.2f,
        float weaponCooldownMultiplier = 0.8f)
        : base("DoubleAction", cooldown)
    {
        this.duration = duration;
        this.moveSpeedBonus = moveSpeedBonus;
        this.weaponCooldownMultiplier = weaponCooldownMultiplier;
    }

    public override void Cast(Player caster)
    {
        if (caster == null) return;

        timer = duration;

        GameManager manager = GameManager.Instance;
        if (manager != null)
            manager.ActivateTeamDoubleAction(duration, moveSpeedBonus, weaponCooldownMultiplier);
    }

    public void Tick(float dt)
    {
        if (timer > 0f)
            timer -= dt;
    }

    public bool IsActive => timer > 0f;
    public float MoveSpeedBonus => moveSpeedBonus;
    public float WeaponCooldownMultiplier => weaponCooldownMultiplier;

    public void AddDuration(float extra)
    {
        duration += extra;
    }

    public void AddMoveSpeedBonus(float extra)
    {
        moveSpeedBonus += extra;
    }

    public void ReduceWeaponCooldown(float amount)
    {
        weaponCooldownMultiplier = Mathf.Max(0.1f, weaponCooldownMultiplier - amount);
    }
}