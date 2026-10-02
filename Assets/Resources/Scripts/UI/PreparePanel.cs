using UnityEngine;
using UnityEngine.UI;

public class PreparePanel : BasePanel //准备面板
{
    public Image imgCharacter;
    public Button btnStart;
    public Button btnSetting;
    public Button btnAchievement;
    public Button btnCharacter;
    public override void Init()
    {
        btnStart.onClick.AddListener(() =>
        {
            UIManager.Instance.HidePanel<PreparePanel>();
            Debug.Log("进入选择章节页面");
        });
        btnSetting.onClick.AddListener(() =>
        {
            UIManager.Instance.ShowPanel<SettingPanel>();
        });
        btnAchievement.onClick.AddListener(() =>
        {
            Debug.Log("进入成就页面");
        });
        btnCharacter.onClick.AddListener(() =>
        {
            Debug.Log("进入查看角色页面");
        });
    }
}
