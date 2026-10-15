using System.Collections.Generic;
using UnityEngine;

/// <summary>天赋选择和胜利结算共享暂停，所有暂停来源释放后才恢复原来的游戏速度。</summary>
public static class TalentPanelPause
{
    private static readonly HashSet<MonoBehaviour> owners = new();
    private static float resumeTimeScale = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        owners.Clear();
        resumeTimeScale = 1f;
    }

    public static void Acquire(MonoBehaviour owner)
    {
        if (owners.Contains(owner)) return;
        if (owners.Count == 0) resumeTimeScale = Time.timeScale;
        owners.Add(owner);
        Time.timeScale = 0f;
    }

    public static void Release(MonoBehaviour owner)
    {
        if (!owners.Remove(owner)) return;
        Time.timeScale = owners.Count == 0 ? resumeTimeScale : 0f;
    }
}
