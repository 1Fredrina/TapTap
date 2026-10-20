using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class HelpPanel : BasePanel
{
    public Button btnExit;
    public override void Init()
    {
        btnExit.onClick.AddListener(() =>
        {
            UIManager.Instance.HidePanel<HelpPanel>();
        });
    }
}
