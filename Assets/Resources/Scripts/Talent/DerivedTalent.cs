using UnityEngine;

public enum DerivedEffect
{
    RegenerationPulse, DodgeInsight, DashRepair,
    // 保留已删除效果的数值空位，避免已有序列化配置错位。
    CriticalLeech = 4, RicochetHunt, MatureExplosion
}

/// <summary>组合前置由原抽取流程判断；配置共享，触发状态保存在角色和子弹上。</summary>
[System.Serializable]
public class DerivedTalent : TalentBase
{
    public DerivedEffect effect;
    public float value, threshold, duration, range, cooldown;
    public int damage;
    public string effectPrefabPath;

    public override void OnAcquire(Player player, int stack)
    {
        player.DerivedEffects.Configure(this);
    }
}
