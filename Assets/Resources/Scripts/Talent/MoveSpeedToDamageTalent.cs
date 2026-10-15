using UnityEngine;

[System.Serializable]
public class MoveSpeedToDamageTalent : TalentBase
{
    public float ratio = 0.5f;   // 每 1.0 移速加成 → 0.5 攻击力加成

    public override void OnAcquire(Player player, int stack)
    {
        if (player == null) return;
        player.SetMoveSpeedToDamageRatio(ratio);
    }
}