using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum AchievementStat { Runs, Kills, Level, TalentChoices, SurvivalSeconds, Victories }

public sealed class AchievementDefinition
{
    public string Id { get; }
    public string Title { get; }
    public string Description { get; }
    public AchievementStat Stat { get; }
    public int Target { get; }

    public AchievementDefinition(string id, string title, string description, AchievementStat stat, int target)
    {
        Id = id;
        Title = title;
        Description = description;
        Stat = stat;
        Target = target;
    }
}

/// <summary>跨局成就存档；单局最高等级和存活时间只取最大值，不跨局相加。</summary>
public sealed class AchievementProgress
{
    private static AchievementProgress instance;
    public static AchievementProgress Instance => instance ??= new AchievementProgress(
        Path.Combine(Application.persistentDataPath, "achievements.json"));

    public static IReadOnlyList<AchievementDefinition> Definitions { get; } = Array.AsReadOnly(new[]
    {
        new AchievementDefinition("first_run", "初次出战", "开始第一场战斗", AchievementStat.Runs, 1),
        new AchievementDefinition("first_kill", "初战告捷", "累计击败 1 个敌人", AchievementStat.Kills, 1),
        new AchievementDefinition("kills_50", "小试身手", "累计击败 50 个敌人", AchievementStat.Kills, 50),
        new AchievementDefinition("kills_200", "清扫能手", "累计击败 200 个敌人", AchievementStat.Kills, 200),
        new AchievementDefinition("first_win", "坚守到底", "存活 20 分钟，获得一次胜利", AchievementStat.Victories, 1),
        new AchievementDefinition("level_5", "渐入佳境", "单局达到 5 级", AchievementStat.Level, 5),
        new AchievementDefinition("level_10", "成长新星", "单局达到 10 级", AchievementStat.Level, 10),
        new AchievementDefinition("first_talent", "天赋初现", "首次在升级面板选择天赋", AchievementStat.TalentChoices, 1),
        new AchievementDefinition("survive_60", "站稳脚跟", "单局存活 1 分钟", AchievementStat.SurvivalSeconds, 60),
        new AchievementDefinition("survive_300", "持久作战", "单局存活 5 分钟", AchievementStat.SurvivalSeconds, 300)
    });

    [Serializable]
    private sealed class SaveData
    {
        public int runs;
        public int kills;
        public int bestLevel;
        public int talentChoices;
        public int bestSurvivalSeconds;
        public int victories;
    }

    private readonly string savePath;
    private SaveData data = new SaveData();
    private bool dirty;
    public event Action Changed;

    // 显式路径允许测试使用隔离存档，不影响玩家进度。
    public AchievementProgress(string savePath)
    {
        this.savePath = savePath;
        try
        {
            if (File.Exists(savePath))
                data = JsonUtility.FromJson<SaveData>(File.ReadAllText(savePath)) ?? new SaveData();
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
        {
            Debug.LogWarning("读取成就存档失败：" + ex.Message);
        }
    }

    public int GetProgress(AchievementDefinition definition)
    {
        int value = definition.Stat switch
        {
            AchievementStat.Runs => data.runs,
            AchievementStat.Kills => data.kills,
            AchievementStat.Level => data.bestLevel,
            AchievementStat.TalentChoices => data.talentChoices,
            AchievementStat.SurvivalSeconds => data.bestSurvivalSeconds,
            AchievementStat.Victories => data.victories,
            _ => 0
        };
        return Mathf.Clamp(value, 0, definition.Target);
    }

    public bool IsUnlocked(AchievementDefinition definition) => GetProgress(definition) >= definition.Target;
    public int UnlockedCount
    {
        get
        {
            int count = 0;
            foreach (var definition in Definitions)
                if (IsUnlocked(definition)) count++;
            return count;
        }
    }

    public void BeginRun() => Increment(ref data.runs);
    public void RecordKill() => Increment(ref data.kills);
    public void RecordTalentChoice() => Increment(ref data.talentChoices);
    public void RecordVictory() => Increment(ref data.victories);
    public void RecordLevel(int level) => RecordMaximum(ref data.bestLevel, Mathf.Max(1, level));
    public void RecordSurvival(float seconds)
    {
        if (float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
        RecordMaximum(ref data.bestSurvivalSeconds, Mathf.Max(0, Mathf.FloorToInt(seconds)));
    }

    private void Increment(ref int value)
    {
        int unlockedBefore = UnlockedCount;
        if (value < int.MaxValue) value++;
        OnChanged(unlockedBefore);
    }

    private void RecordMaximum(ref int value, int next)
    {
        if (next <= value) return;
        int unlockedBefore = UnlockedCount;
        value = next;
        OnChanged(unlockedBefore);
    }

    private void OnChanged(int unlockedBefore)
    {
        dirty = true;
        // 解锁立即落盘，普通进度由 GameManager 定期及结束时保存，避免每次击杀写磁盘。
        if (UnlockedCount > unlockedBefore) Save();
        Changed?.Invoke();
    }

    public void Save()
    {
        if (!dirty) return;
        try
        {
            string directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(savePath, JsonUtility.ToJson(data, true));
            dirty = false;
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            Debug.LogWarning("保存成就进度失败：" + ex.Message);
        }
    }
}
