using System.Collections.Generic;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    [Header("关卡设置")]
    [Tooltip("当前关卡的 ID，必须和 LevelsData.json 里的 LevelsID 对应")]
    [SerializeField] private int currentLevelId = 1;

    [Tooltip("勾选则自动在 Start 时查找场景里所有 Enemy")]
    [SerializeField] private bool autoFindEnemies = true;

    [Tooltip("手动指定敌人列表（autoFindEnemies = false 时使用）")]
    [SerializeField] private List<Enemy> enemies = new List<Enemy>();

    private bool isCompleted = false;

    private void Start()
    {
        if (autoFindEnemies)
            RefreshEnemyList();
    }

    private void Update()
    {
        if (isCompleted) return;
        if (enemies == null || enemies.Count == 0) return;

        if (AllEnemiesDefeated())
            OnLevelComplete();
    }

    private void RefreshEnemyList()
    {
        enemies.Clear();
        Enemy[] all = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        enemies.AddRange(all);
    }


    private bool AllEnemiesDefeated()
    {
        foreach (var e in enemies)
        {
            if (e == null) continue;

            if (!e.IsDead && e.gameObject.activeInHierarchy)
                return false;
        }
        return true;
    }


    private void OnLevelComplete()
    {
        isCompleted = true;
        Debug.Log($"[Level {currentLevelId}] 全部敌人已清空，通关！");

        UnlockNextLevel();

        //UIManager.Instanse.ShowPanel<LevelCompletePanel>();
    }

    private void UnlockNextLevel()
    {
        int nextId = currentLevelId + 1;
        var list = LevelsMgr.Instance.levelsDatas;
        if (list == null) return;

        bool changed = false;
        bool nextExists = false;

        foreach (var data in list)
        {
            if (data.LevelsID == nextId)
            {
                nextExists = true;
                if (!data.IsUnlock)
                {
                    data.IsUnlock = true;
                    changed = true;
                }
            }
        }

        if (!nextExists)
        {
            return;
        }

        if (changed)
        {
            JsonMgr.Instance.SaveData(list, "LevelsData");
        }
    }
}