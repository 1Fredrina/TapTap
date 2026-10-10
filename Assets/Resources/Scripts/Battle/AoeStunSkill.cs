using UnityEngine;

public class AoeStunSkill : SkillBase
{
    private GameObject effectPrefab;
    private float stunDuration;

    public AoeStunSkill(GameObject prefab, float stunDuration = 2f, float cooldown = 8f)
        : base("AoeStun", cooldown)
    {
        this.effectPrefab = prefab;
        this.stunDuration = stunDuration;
    }

    public override void Cast(Player caster)
    {
        if (effectPrefab == null)
        {
            Debug.LogWarning("[AoeStun] effectPrefab is null");
            return;
        }

        GameObject effect = Object.Instantiate(effectPrefab, caster.transform.position, Quaternion.identity);

        if (effect.TryGetComponent<AoeStunEffect>(out var comp))
            comp.Init(stunDuration);

        Debug.Log($"[AoeStun] Cast, stun {stunDuration}s");
    }
}