[System.Serializable]
public class KillFrenzyTalent : TalentBase
{
    public float duration = 4f;
    public int maxCharges = 5;
    public float attackSpeedPerCharge = 0.04f;
    public float moveSpeedPerCharge = 0.02f;

    public override void OnAcquire(Player player, int stack)
    {
        if (player == null) return;
        player.SetKillFrenzy(duration, maxCharges, attackSpeedPerCharge * stack, moveSpeedPerCharge * stack);
    }
}
