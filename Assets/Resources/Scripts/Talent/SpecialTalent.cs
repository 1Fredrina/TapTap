using UnityEngine;

public enum SpecialEffect
{
    CursedBlade,
    EchoForm,
    Pentagram,
    HealthToShield,
    ShieldToHealth,
    VitalityPower,
    ShieldPower
}

[System.Serializable]
public class SpecialTalent : TalentBase
{
    public SpecialEffect effect;

    public override void OnAcquire(Player player, int stack)
    {
        if (player != null)
            player.SpecialEffects.Configure(this);
    }
}
