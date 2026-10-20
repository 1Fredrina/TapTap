using UnityEngine;

public class LevelController : MonoBehaviour
{
    [Header("关卡设置")]
    [Tooltip("当前关卡的 ID，必须和 LevelsData.json 里的 LevelsID 对应")]
    [SerializeField] private int currentLevelId = 1;

    public int CurrentLevelId => currentLevelId;
    public bool IsCompleted { get; private set; }

    // 生存关卡持续刷怪，由 GameManager 的胜利流程明确通知通关。
    public void CompleteLevel()
    {
        GameManager manager = GameManager.Instance;
        if (IsCompleted || manager == null || !manager.IsVictory || manager.gameObject.scene != gameObject.scene)
            return;

        IsCompleted = true;
        LevelsMgr.Instance.UnlockNextLevel(currentLevelId);
        Debug.Log($"[Level {currentLevelId}] 生存挑战通关！");
    }
}
