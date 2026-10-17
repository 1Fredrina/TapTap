using UnityEngine;
using UnityEngine.UI;

public class AchievementPanel : BasePanel
{
    public Button btnExit;
    public Text txtSummary;
    public Text[] achievementTexts = new Text[10];

    private void OnEnable()
    {
        AchievementProgress.Instance.Changed += RefreshProgress;
        RefreshProgress();
    }

    private void OnDisable()
    {
        AchievementProgress.Instance.Changed -= RefreshProgress;
    }

    public override void ShowMe()
    {
        base.ShowMe();
        RefreshProgress();
    }

    public void RefreshProgress()
    {
        AchievementProgress progress = AchievementProgress.Instance;
        if (txtSummary != null)
            txtSummary.text = $"已完成成就  {progress.UnlockedCount} / {AchievementProgress.Definitions.Count}";

        for (int i = 0; i < AchievementProgress.Definitions.Count; i++)
        {
            if (achievementTexts == null || i >= achievementTexts.Length || achievementTexts[i] == null) continue;
            var definition = AchievementProgress.Definitions[i];
            bool unlocked = progress.IsUnlocked(definition);
            string state = unlocked ? "<color=#317A39>已完成</color>" : "<color=#666666>未完成</color>";
            string unit = definition.Stat == AchievementStat.SurvivalSeconds ? " 秒" : "";
            achievementTexts[i].text = $"<b>{definition.Title}</b>  {state}\n{definition.Description}  {progress.GetProgress(definition)}/{definition.Target}{unit}";
        }
    }

    public override void Init()
    {
        if (btnExit != null) btnExit.onClick.AddListener(() =>
        {
            UIManager.Instance.HidePanel<AchievementPanel>();
            UIManager.Instance.ShowPanel<PreparePanel>();
        });
    }


}
