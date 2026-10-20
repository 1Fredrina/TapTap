using System;
using System.Collections.Generic;
using System.IO;
using LitJson;
using UnityEngine;

public sealed class LevelsMgr
{
    private static LevelsMgr instance;
    public static LevelsMgr Instance => instance ??= new LevelsMgr(
        Path.Combine(Application.persistentDataPath, "LevelsData.json"));

    public readonly List<LevelsData> levelsDatas;
    private readonly string savePath;

    // 显式路径用于隔离测试存档，正式游戏使用 persistentDataPath。
    public LevelsMgr(string savePath)
    {
        this.savePath = savePath;
        levelsDatas = JsonMgr.Instance.LoadData<List<LevelsData>>("LevelsData") ?? new List<LevelsData>();

        // 配置提供完整关卡列表，存档只合并解锁进度，避免重启后被默认锁定状态覆盖。
        try
        {
            if (File.Exists(savePath))
            {
                var saved = JsonMapper.ToObject<List<LevelsData>>(File.ReadAllText(savePath));
                if (saved != null)
                    foreach (var data in saved)
                    {
                        if (data == null || !data.IsUnlock) continue;
                        var level = levelsDatas.Find(item => item != null && item.LevelsID == data.LevelsID);
                        if (level != null) level.IsUnlock = true;
                    }
            }
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is JsonException)
        {
            Debug.LogWarning("读取关卡进度失败：" + ex.Message);
        }

        var first = levelsDatas.Find(data => data != null && data.LevelsID == 1);
        if (first == null)
            levelsDatas.Add(new LevelsData { LevelsID = 1, IsUnlock = true });
        else
            first.IsUnlock = true;
    }

    public bool IsLevelUnlocked(int levelId) =>
        levelsDatas.Exists(data => data != null && data.LevelsID == levelId && data.IsUnlock);

    public bool UnlockNextLevel(int currentLevelId)
    {
        if (currentLevelId < 1 ||
            !levelsDatas.Exists(data => data != null && data.LevelsID == currentLevelId)) return false;

        int nextId = currentLevelId + 1;
        var next = levelsDatas.Find(data => data != null && data.LevelsID == nextId);
        // 数据中预留的关卡没有可加载场景时，不解锁空入口。
        if (next == null || next.IsUnlock || !Application.CanStreamedLevelBeLoaded("Level" + nextId)) return false;

        next.IsUnlock = true;
        try
        {
            string directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(savePath, JsonMapper.ToJson(levelsDatas));
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            Debug.LogWarning("保存关卡进度失败：" + ex.Message);
        }
        return true;
    }
}
