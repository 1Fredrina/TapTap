using UnityEngine;

[System.Serializable]
public class EnemyData
{
    public int id;
    public string name;
    [Min(1f)] public float MaxHealth = 10f;
    [Min(0f)] public float MoveSpeed = 1f;
    [Min(0)] public int AttackDamage = 10;
    [Min(1)] public int ExperienceReward = 100;
    [Min(0f)] public float AttackInterval = 1f;
}
