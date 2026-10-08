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
            UIManager.Instance.ShowPanel<LevelPanel>();
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
            UIManager.Instance.HidePanel<PreparePanel>();
            UIManager.Instance.ShowPanel<CharacterPanel>();
        });
    }
}
