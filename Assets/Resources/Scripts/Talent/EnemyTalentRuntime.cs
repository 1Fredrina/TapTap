using System.Collections.Generic;
using UnityEngine;

/// <summary>每局独立的敌人异变进度，不修改 EnemyData 或共享天赋配置。</summary>
public sealed class EnemyTalentRuntime
{
    private readonly float interval;
    private readonly List<EnemyTalentData> pendingChoices = new();
    private readonly Dictionary<string, int> stacks = new();

    public EnemyTalentRuntime(float interval = GameManager.EnemyTalentInterval)
    {
        this.interval = Mathf.Max(1f, interval);
        NextChoiceTime = this.interval;
    }

    public float ElapsedTime { get; private set; }
    public float NextChoiceTime { get; private set; }
    public int UpgradeCount { get; private set; }
    public float HealthMultiplier { get; private set; } = 1f;
    public float DamageMultiplier { get; private set; } = 1f;
    public float MoveSpeedMultiplier { get; private set; } = 1f;
    public float DamageTakenMultiplier { get; private set; } = 1f;
    public bool IsChoosing => pendingChoices.Count > 0;
    public bool IsChoiceDue => ElapsedTime >= NextChoiceTime;
    public IReadOnlyList<EnemyTalentData> PendingChoices => pendingChoices;

    public void Tick(float deltaTime)
    {
        if (deltaTime > 0f && !IsChoosing)
            ElapsedTime += deltaTime;
    }

    public List<EnemyTalentData> RollChoices(IReadOnlyList<EnemyTalentData> pool, int count = 3)
    {
        if (!IsChoiceDue || IsChoosing || pool == null) return new List<EnemyTalentData>(pendingChoices);
        var candidates = new List<EnemyTalentData>();
        var ids = new HashSet<string>();
        foreach (var talent in pool)
            if (talent != null && !string.IsNullOrEmpty(talent.id) && ids.Add(talent.id))
                candidates.Add(talent);

        while (pendingChoices.Count < count && candidates.Count > 0)
        {
            int index = Random.Range(0, candidates.Count);
            pendingChoices.Add(candidates[index]);
            candidates.RemoveAt(index);
        }
        return new List<EnemyTalentData>(pendingChoices);
    }

    public bool Choose(EnemyTalentData talent)
    {
        if (talent == null || !pendingChoices.Contains(talent)) return false;
        HealthMultiplier *= Mathf.Max(1f, talent.healthMultiplier);
        DamageMultiplier *= Mathf.Max(1f, talent.damageMultiplier);
        MoveSpeedMultiplier *= Mathf.Max(1f, talent.moveSpeedMultiplier);
        DamageTakenMultiplier *= Mathf.Clamp(talent.damageTakenMultiplier, 0.01f, 1f);
        stacks[talent.id] = GetStack(talent.id) + 1;
        UpgradeCount++;
        pendingChoices.Clear();
        NextChoiceTime += interval;
        return true;
    }

    public int GetStack(string id) => stacks.TryGetValue(id, out int count) ? count : 0;
    public void CancelChoice() => pendingChoices.Clear();
}
