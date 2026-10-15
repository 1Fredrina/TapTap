using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public static class CodexSaveManager
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "codex.json");
    private static SaveData _cache;

    // 极简存档结构
    [System.Serializable]
    private class SaveData { public List<Record> records = new List<Record>(); }

    [System.Serializable]
    private class Record { public string id; public int kills; }

    private static SaveData Load()
    {
        if (_cache != null) return _cache;
        if (File.Exists(SavePath)) _cache = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        if (_cache == null) _cache = new SaveData();
        return _cache;
    }

    private static void Save() => File.WriteAllText(SavePath, JsonUtility.ToJson(Load(), true));

    // --- 对外接口 ---

    public static void RecordKill(string id)
    {
        var data = Load();
        var rec = data.records.FirstOrDefault(r => r.id == id);
        if (rec == null) { rec = new Record { id = id, kills = 0 }; data.records.Add(rec); }
        rec.kills++;
        Save();
    }

    public static bool IsUnlocked(string id) => Load().records.Any(r => r.id == id);

    public static int GetKillCount(string id) => Load().records.FirstOrDefault(r => r.id == id)?.kills ?? 0;

    public static int GetUnlockedCount() => Load().records.Count;
}