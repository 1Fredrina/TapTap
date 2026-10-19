using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    // 在 Inspector 中拖拽绑定主界面上的"图鉴"按钮
    public UnityEngine.UI.Button codexButton;

    void Start()
    {
        if (codexButton != null)
        {
            codexButton.onClick.AddListener(OnCodexButtonClick);
        }
        else
        {
            Debug.LogError("[MainMenu] ❌ 未绑定图鉴按钮，请在 Inspector 中拖拽赋值！");
        }
    }

    private void OnCodexButtonClick()
    {
        UIManager.Instance.ShowPanel<CodexUIManager>();
    }
}