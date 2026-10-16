using UnityEngine;

public enum AdvancedEffect
{
    DamageEcho, DashBloom, KillBank, StillnessCharge, LowHealthBurst,
    StunBattery, ShieldThorns, PhaseShift, ExecutionMark, AmmoCycle,
    ApexProtocol, BioSingularity, ImmortalCycle, ArsenalOverdrive, PerfectHunt
}

[System.Serializable]
public class AdvancedTalent : TalentBase
{
    public AdvancedEffect effect;
    public float value, threshold, duration, range, cooldown, window, healingThreshold;
    public int damage;
    public bool terminal;
    public string effectPrefabPath;

    public override void OnAcquire(Player player, int stack)
    {
        if (player != null) player.AdvancedEffects.Configure(this, stack);
    }
}
