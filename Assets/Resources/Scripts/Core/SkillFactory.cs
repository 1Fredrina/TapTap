using UnityEngine;

public static class SkillFactory
{
    public static SkillBase Create(string id)
    {
        switch (id)
        {
            case "Heal":   return new HealSkill(2);
            case "DamageBuff": return new DamageBuffSkill(2f, 4f);
            case "DoubleAction": return new DoubleActionSkill(10f);
            case "AoeStun":
                {
                    GameObject prefab = Resources.Load<GameObject>("Prefabs/Skills/AoeStunEffect");
                    return new AoeStunSkill(prefab, 2f, 8f);
                }
            default:
                Debug.LogWarning($"Unknown skill id: {id}");
                return null;
        }
    }
}