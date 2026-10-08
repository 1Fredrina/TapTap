using UnityEngine;

[System.Serializable]
public class PlayerData
{
    public int id;
    public string name;
    [Min(0f)] public float MoveSpeed = 6f;
    [Min(0f)] public float JumpHeight = 2.5f;
    [Min(0f)] public float AttackInterval = 0.5f;
}
