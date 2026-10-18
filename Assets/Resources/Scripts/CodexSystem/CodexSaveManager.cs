using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public static class CodexSaveManager
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "codex.json");
    private static SaveData _cache;

#if UNITY_EDITOR
    // 🧪 使用 EditorPrefs 持久化测试开关，避免进入播放模式时静态变量被重置
    private const string TEST_MODE_KEY = "Codex_ForceUnlockAllForTest";
    private static bool ForceUnlockAllForTest
    {
        get => UnityEditor.EditorPrefs.GetBool(TEST_MODE_KEY, false);
        set => UnityEditor.EditorPrefs.SetBool(TEST_MODE_KEY, value);
    }
#endif

    // 极简存档结构
    [System.Serializable]
    private class SaveData { public List<Record> records = new List<Record>(); }

    [System.Serializable]
    private class Record { public int id; public int kills; }

    private static SaveData Load()
    {
        if (_cache != null) return _cache;
        if (File.Exists(SavePath)) _cache = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        if (_cache == null) _cache = new SaveData();
        return _cache;
    }

    private static void Save() => File.WriteAllText(SavePath, JsonUtility.ToJson(Load(), true));

    // --- 对外接口 ---

    public static void RecordKill(int id)
    {
        var data = Load();
        var rec = data.records.FirstOrDefault(r => r.id == id);
        if (rec == null) { rec = new Record { id = id, kills = 0 }; data.records.Add(rec); }
        rec.kills++;
        Save();
    }

    public static bool IsUnlocked(int id)
    {
#if UNITY_EDITOR
        if (ForceUnlockAllForTest) return true;
#endif
        return Load().records.Any(r => r.id == id);
    }

    public static int GetKillCount(int id)
    {
#if UNITY_EDITOR
        // 测试模式下返回模拟击杀数，避免UI显示"击杀数: 0"穿帮
        if (ForceUnlockAllForTest) return 99;
#endif
        return Load().records.FirstOrDefault(r => r.id == id)?.kills ?? 0;
    }

    public static int GetUnlockedCount()
    {
#if UNITY_EDITOR
        // 测试模式下返回一个合理的模拟值，避免依赖真实记录数
        if (ForceUnlockAllForTest) return 99;
#endif
        return Load().records.Count;
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Codex/切换图鉴测试模式")]
    public static void ToggleTestMode()
    {
        bool newState = !ForceUnlockAllForTest;
        ForceUnlockAllForTest = newState;

        string state = newState ? "🟢 已开启" : "🔴 已关闭";
        Debug.Log($"[Codex] 测试模式{state}（状态已持久化，进入播放模式依然生效）");

        // 关闭测试时清除缓存，确保恢复真实数据
        if (!newState) _cache = null;
    }

    [UnityEditor.MenuItem("Tools/Codex/⚠️ 查看当前测试模式状态", true)]
    public static bool ShowTestModeStatus()
    {
        bool isOn = UnityEditor.EditorPrefs.GetBool(TEST_MODE_KEY, false);
        Debug.Log($"[Codex] 当前测试模式: {(isOn ? "🟢 开启中" : "🔴 已关闭")}");
        return false; // 返回 false 使该菜单项始终置灰不可点，仅作状态提示
    }

    [UnityEditor.MenuItem("Tools/Codex/清除图鉴解锁存档")]
    public static void ClearAllCodexProgress()
    {
        // 1. 删除磁盘上的 JSON 文件
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log($"[Codex] ✅ 已删除存档文件: {SavePath}");
        }
        else
        {
            Debug.Log("[Codex] ⚠️ 未找到存档文件，无需删除");
        }

        // 2. 清空内存缓存（关键！否则下次 Load 仍会返回旧数据）
        _cache = null;

        Debug.Log("[Codex] ✅ 图鉴存档已彻底重置，重新运行游戏即可验证未解锁状态");
    }
#endif
}