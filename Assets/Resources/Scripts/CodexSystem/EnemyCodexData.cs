using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemy", menuName = "Codex/Enemy Data")]
public class EnemyCodexData : ScriptableObject
{
    public string enemyID;       // 唯一标识（必须和代码里调用RecordKill时的名字完全一致）
    public string displayName;   // 显示名称
    public Sprite icon;          // 头像图标
    [TextArea] public string desc; // 描述
}