using UnityEngine;

[System.Serializable]
public class PlayerData
{
    public int id;
    public string name;
    [Min(0f)] public float MoveSpeed = 6f;
    [Min(0f)] public float JumpHeight = 2.5f;
    public SkillBase entrySkill;       // 切换到该角色时，该角色释放什么
    public AssistEffect outgoingAssist; // 该角色切换出去时，施加什么强化
}
