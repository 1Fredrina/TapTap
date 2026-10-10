using UnityEngine;

[System.Serializable]
public class EnemyData
{
    public int id;
    public string name;
    [Min(1f)] public float MaxHealth = 10f;
    [Min(0f)] public float MoveSpeed = 2f;
    [Min(0)] public int AttackDamage = 1;
    [Min(0f)] public float AttackInterval = 1f;
}
