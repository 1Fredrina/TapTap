using UnityEngine;

[System.Serializable]
public class StationaryTalent : TalentBase
{
    public float attackSpeedMax = 1.5f;
    public float sizeMax = 0.5f;

    public override void OnAcquire(Player player, int stack)
    {
        if (player == null) return;
        player.SetStationaryBonus(attackSpeedMax, sizeMax);
    }
}