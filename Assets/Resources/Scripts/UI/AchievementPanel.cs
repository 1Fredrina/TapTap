using UnityEngine;
using UnityEngine.UI;

public class AchievementPanel : BasePanel
{
    public Button btnExit;
    public override void Init()
    {
        btnExit.onClick.AddListener(() =>
        {
            UIManager.Instance.HidePanel<AchievementPanel>();
            UIManager.Instance.ShowPanel<PreparePanel>();
        });
    }


}
