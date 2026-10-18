using UnityEngine;

public enum SpecialEffect
{
    CursedBlade,
    EchoForm,
    Pentagram,
    HealthToShield,
    ShieldToHealth,
    VitalityPower,
    ShieldPower,
    Brimstone
}

[System.Serializable]
public class SpecialTalent : TalentBase
{
    public SpecialEffect effect;
    public float brimstoneCooldown = 4f;
    public int brimstoneDamage = 30;
    public float brimstoneRange = 18f;
    public float brimstoneWidth = 0.75f;
    public float brimstoneDuration = 0.16f;

    public override void OnAcquire(Player player, int stack)
    {
        if (player != null)
            player.SpecialEffects.Configure(this);
    }
}
