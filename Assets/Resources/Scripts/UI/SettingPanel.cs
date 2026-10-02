using UnityEngine;
using UnityEngine.UI;

public class SettingPanel : BasePanel //设置面板，提供音量调节、游戏说明
{
    public Button btnClose;
    public Button btnExit;
    public Slider sliderMusic;
    public Slider sliderSound;
    public override void Init()
    {
        btnClose.onClick.AddListener(() =>
        {
            UIManager.Instance.HidePanel<SettingPanel>();
        });
        btnExit.onClick.AddListener(() =>
        {
            Application.Quit();
        });
    }
}
