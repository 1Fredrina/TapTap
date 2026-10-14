using UnityEngine;

[System.Serializable]
public class CompoundTalent : TalentBase
{
    public StatModifier[] statModifiers;

    public override void OnAcquire(Player player, int stack)
    {
        if (player == null) return;

        foreach (var mod in statModifiers)
        {
            player.ApplyStatTalent(mod.statType, mod.value, stack);
        }
    }
}

[System.Serializable]
public class StatModifier
{
    public StatType statType;
    public float value;
}