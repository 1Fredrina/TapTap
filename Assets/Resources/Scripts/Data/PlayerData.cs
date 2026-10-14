using UnityEngine;

[System.Serializable]
public class PlayerData
{
    public int id;
    public string name;
    [Min(0f)] public float MoveSpeed = 6f;
    [Min(1f)] public float MaxHealth = 100f;
    [Min(0f)] public float AttackInterval = 0.5f;
    [Min(1)] public int AttackDamage = 1;
    [Min(0f)] public float AttackRange = 12f;
    [Min(1)] public int ProjectileCount = 1;
    [Range(0f, 180f)] public float ProjectileSpread = 12f;
    [Min(0f)] public float PickupRange = 2.5f;
    [Min(0f)] public float ExperienceMultiplier = 1f;
    [Min(1)] public int BaseExperienceToNextLevel = 5;
    [Min(0f)] public float InvincibleTime = 1f;
    [Min(0f)] public float StunTime = 0.8f;
    public string skillId;
    [Min(0f)] public float skillCooldown = 6f;
    [Min(0f)] public float DashCooldown = 1f;

    [Header("护盾")]
    [Min(0f)] public float ShieldDecayRate = 0.1f;

    [Header("等级")]
    [Min(1)] public int MaxLevel = 21;

    [Header("天赋池")]
    public TalentBase[] talents;
    public string startingWeaponId;
}