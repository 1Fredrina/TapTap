using UnityEngine;
using UnityEngine.UI;

public class BeginPanel : BasePanel //游戏开始界面，点击开始按钮后，进入游戏
{
    public Button btnStart;
    public Button btnSetting;
    public Button btnExit;
    public override void Init()
    {
        btnStart.onClick.AddListener(() =>
        {
            UIManager.Instance.HidePanel<BeginPanel>();
            UIManager.Instance.ShowPanel<PreparePanel>();
        });
        btnSetting.onClick.AddListener(() =>
        {
            UIManager.Instance.ShowPanel<SettingPanel>();         
        });
        btnExit.onClick.AddListener(() =>
        {
            Application.Quit();
        });
    }
}
