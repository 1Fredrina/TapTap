using System;

/// <summary>敌人异变的只读配置；叠层和倍率保存在本局运行时中。</summary>
[Serializable]
public class EnemyTalentData
{
    public string id;
    public string displayName;
    public string description;
    public float healthMultiplier = 1f;
    public float damageMultiplier = 1f;
    public float moveSpeedMultiplier = 1f;
    public float damageTakenMultiplier = 1f;
}
