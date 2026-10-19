using UnityEngine;
using UnityEngine.UI;

public class PlayerStatusUI : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Image shieldBarFill;
    [SerializeField] private Image expBarFill;

    [Header("文本引用")]
    [SerializeField] private Text healthText;
    [SerializeField] private Text levelText;
    [SerializeField] private Text timeText;   // 新增：对局时长

    [Header("平滑过渡参数")]
    [SerializeField, Min(0.1f)] private float smoothSpeed = 10f;

    private Player player;

    // 记录当前显示的进度
    private float currentHealthFill = 1f;
    private float currentShieldFill = 0f;
    private float currentExpFill = 0f;

    // 对局计时
    private float matchTime = 0f;

        private void Update()
    {
        // 1. 更新对局时长 (受 timeScale 影响，暂停时停止)
        if (GameManager.Instance == null || !GameManager.Instance.IsGameOver)
        {
            matchTime += Time.deltaTime; // 这里改成 deltaTime
        }

        if (timeText != null)
        {
            int minutes = Mathf.FloorToInt(matchTime / 60f);
            int seconds = Mathf.FloorToInt(matchTime % 60f);
            timeText.text = $"时间: {minutes:00}:{seconds:00}";
        }

        // 2. 获取玩家并刷新 UI (UI 动画依然用 unscaledDeltaTime 保持平滑)
        if (player == null)
        {
            if (GameManager.Instance != null && GameManager.Instance.Player != null)
                player = GameManager.Instance.Player;
            else
                return;
        }

        RefreshUI();
    }

    private void RefreshUI()
    {
        // ===== 1. 血条 =====
        float targetHealthRatio = player.MaxHealth > 0f ? (player.CurrentHealth / player.MaxHealth) : 0f;
        currentHealthFill = Mathf.Lerp(currentHealthFill, targetHealthRatio, Time.unscaledDeltaTime * smoothSpeed);
        
        if (healthBarFill != null)
            healthBarFill.fillAmount = currentHealthFill;

        // ===== 2. 护盾条 (有盾就全满) =====
        float targetShieldRatio = (player.CurrentShield > 0f) ? 1f : 0f;
        if (shieldBarFill != null)
            shieldBarFill.fillAmount = targetShieldRatio;

        // ===== 3. 文本 =====
        if (healthText != null)
        {
            int hp = Mathf.CeilToInt(player.CurrentHealth);
            int maxHp = Mathf.CeilToInt(player.MaxHealth);
            int shield = Mathf.CeilToInt(player.CurrentShield);

            if (shield > 0)
                healthText.text = $"血量 {hp}/{maxHp} | <color=#00BFFF>护盾 {shield}</color>";
            else
                healthText.text = $"血量 {hp}/{maxHp}";
        }

        if (levelText != null)
        {
            levelText.text = $"等级: Lv.{player.Level}";
        }

        // ===== 4. 经验条 =====
        float targetExpRatio = player.ExperienceToNextLevel > 0 
            ? (float)player.Experience / player.ExperienceToNextLevel 
            : 0f;
            
        if (targetExpRatio < currentExpFill)
            currentExpFill = targetExpRatio;
        else
            currentExpFill = Mathf.Lerp(currentExpFill, targetExpRatio, Time.unscaledDeltaTime * smoothSpeed);

        if (expBarFill != null)
            expBarFill.fillAmount = currentExpFill;
    }
}