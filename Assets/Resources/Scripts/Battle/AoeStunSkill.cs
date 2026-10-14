using UnityEngine;

public class AoeStunSkill : SkillBase
{
    private GameObject effectPrefab;
    private float stunDuration;

    public AoeStunSkill(GameObject prefab, float stunDuration = 3f, float cooldown = 15f)
        : base("AoeStun", cooldown)
    {
        this.effectPrefab = prefab;
        this.stunDuration = stunDuration;
    }

    public override void Cast(Player caster)
    {
        if (caster == null) return;

        if (effectPrefab == null)
        {
            return;
        }

        GameObject effect = Object.Instantiate(effectPrefab, caster.transform.position, Quaternion.identity);

        if (effect.TryGetComponent<AoeStunEffect>(out var comp))
            comp.Init(stunDuration);

    }

    public float StunDuration => stunDuration;

    public void AddStunDuration(float extra)
    {
        stunDuration += extra;
    }
}