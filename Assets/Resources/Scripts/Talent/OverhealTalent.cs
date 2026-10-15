[System.Serializable]
public class OverhealTalent : TalentBase
{
    public float shieldRatio = 0.5f;
    public float shieldCapPercent = 0.3f;

    public override void OnAcquire(Player player, int stack)
    {
        if (player == null) return;
        player.SetOverhealShield(shieldRatio * stack, shieldCapPercent);
    }
}
