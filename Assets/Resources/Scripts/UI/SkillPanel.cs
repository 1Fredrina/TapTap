using UnityEngine;
using UnityEngine.UI;

public class SkillPanel : BasePanel
{
    public Button btnExit;
    public Text txtSkill;
    public override void Init()
    {
        btnExit.onClick.AddListener(() => { 
            UIManager.Instance.HidePanel<SkillPanel>();
         });
    }
}
