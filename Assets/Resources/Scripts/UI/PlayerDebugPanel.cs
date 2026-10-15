using System.Text;
using UnityEngine;

public class PlayerDebugPanel : MonoBehaviour
{
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;
    [SerializeField] private bool visible = true;

    private GUIStyle style;
    private StringBuilder sb = new StringBuilder();

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            visible = !visible;
    }

    private void OnGUI()
    {
        if (!visible || Time.timeScale <= 0f) return;

        Player player = GameManager.Instance != null ? GameManager.Instance.Player : null;
        if (player == null) return;

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label);
            style.fontSize = 14;
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.UpperLeft;
        }

        float rangeBonus = 1f + player.GetStatBonus(StatType.AttackRange);

        sb.Clear();
        sb.AppendLine("=== 角色属性 (F1) ===");
        sb.AppendLine($"等级：{player.Level} / {player.Data.MaxLevel}");
        sb.AppendLine($"经验：{player.Experience} / {player.ExperienceToNextLevel}");
        sb.AppendLine($"生命：{player.CurrentHealth:F1} / {player.MaxHealth:F1}");
        sb.AppendLine($"护盾：{player.CurrentShield:F1}");
        sb.AppendLine();
        sb.AppendLine($"攻击力：{player.AttackDamage * player.DamageMultiplier:F2}");
        sb.AppendLine($"攻击间隔：{player.AttackInterval:F2} 秒");
        sb.AppendLine($"武器冷却倍率：{player.WeaponCooldownMultiplier:F2}");
        sb.AppendLine($"移动速度加成：{player.MoveSpeedBonus * 100:F0}%");
        if (player.KillFrenzyCharges > 0)
            sb.AppendLine($"连锁活化：{player.KillFrenzyCharges} 层 / 剩余 {player.KillFrenzyTimeRemaining:F1} 秒");
        var effects = player.TalentEffects;
        if (effects != null)
        {
            if (effects.TravelCharge > 0f)
                sb.AppendLine($"游走电容：{effects.TravelCharge * 100f:F0}%");
            if (effects.KillsPerNova > 0)
                sb.AppendLine($"凋亡新星：{effects.NovaKills} / {effects.KillsPerNova}");
            if (effects.HasLastStand)
                sb.AppendLine(effects.LastStandCooldownRemaining > 0f
                    ? $"绝境蜕生：冷却 {effects.LastStandCooldownRemaining:F1} 秒" : "绝境蜕生：就绪");
        }
        sb.AppendLine();
        sb.AppendLine($"无敌时间：{player.Data.InvincibleTime * (1f + player.GetStatBonus(StatType.InvincibleTime)):F2} 秒");
        sb.AppendLine($"护盾衰减：{player.Data.ShieldDecayRate * (1f + player.GetStatBonus(StatType.ShieldDecay)):F3} / 秒");
        sb.AppendLine($"受击减伤：{Mathf.Clamp(player.GetStatBonus(StatType.DamageReduction), 0f, 0.9f) * 100:F0}%");
        sb.AppendLine($"技能冷却倍率：{1f + player.GetStatBonus(StatType.SkillCooldown):F2}");
        sb.AppendLine($"击杀回血：{player.GetStatBonus(StatType.HealOnKill):F1}");
        sb.AppendLine($"击杀回盾：{player.GetStatBonus(StatType.ShieldOnKill):F1}");
        sb.AppendLine($"经验倍率：{player.Data.ExperienceMultiplier * (1f + player.GetStatBonus(StatType.ExperienceGain)):F2}");
        sb.AppendLine($"额外选项：{player.GetStatBonus(StatType.ExtraChoice):F0}");
        sb.AppendLine();
        sb.AppendLine($"红色天赋：{player.GetColorCount(TalentColor.Red)} / {TalentBase.MaxRed}");
        sb.AppendLine($"蓝色天赋：{player.GetColorCount(TalentColor.Blue)} / {TalentBase.MaxBlue}");
        sb.AppendLine($"绿色天赋：{player.GetColorCount(TalentColor.Green)} / {TalentBase.MaxGreen}");
        sb.AppendLine();
        sb.AppendLine($"武器数量：{player.Weapons.Count}");
        foreach (var w in player.Weapons)
        {
            if (w == null) continue;
            float dmg = w.damage * player.DamageMultiplier;
            float cd = Mathf.Max(0.05f, w.cooldown - (w.stack - 1) * 0.05f) * player.WeaponCooldownMultiplier;
            float rng = w.range * rangeBonus;
            sb.AppendLine($"  - {w.displayName} 层数{w.stack} 伤害{dmg:F1} 冷却{cd:F2}s 范围{rng:F1}");
        }
        sb.AppendLine();
        sb.AppendLine($"天赋数量：{player.AcquiredTalents.Count}");
        foreach (var t in player.AcquiredTalents)
        {
            if (t.data == null) continue;
            sb.AppendLine($"  - {t.data.displayName} x{t.stack}");
        }

        GUI.Label(new Rect(10, 10, 600, 900), sb.ToString(), style);
    }
}
