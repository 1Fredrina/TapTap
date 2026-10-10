using UnityEngine;

[System.Serializable]
public class PlayerData
{
    public int id;
    public string name;
    [Min(0f)] public float MoveSpeed = 6f;
    [Min(0f)] public float JumpHeight = 2.5f;
    [Min(0f)] public float AttackInterval = 0.5f;
    [Min(0f)] public float InvincibleTime = 1f;
    [Min(0f)] public float KnockbackForce = 8f;
    [Min(0f)] public float KnockbackUpForce = 4f;
    [Min(0f)] public float StunTime = 0.8f;
    public string skillId;
    [Min(0f)] public float skillCooldown = 6f;
    [Min(0f)] public float DashCooldown = 1f;
}
