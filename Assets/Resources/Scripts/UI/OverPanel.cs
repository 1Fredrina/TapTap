using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class OverPanel : BasePanel
{
    public Button btnExit;
    public Button btnRestart;

    [FormerlySerializedAs("txtover")]
    public Text txtOver;

    public override void Init()
    {
        if (btnExit != null)
        {
            btnExit.onClick.RemoveAllListeners();
            btnExit.onClick.AddListener(ReturnToMenu);
        }

        if (btnRestart != null)
        {
            btnRestart.onClick.RemoveAllListeners();
            btnRestart.onClick.AddListener(RestartLevel);
        }
    }

    public void ShowResult(bool victory)
    {
        if (txtOver != null)
            txtOver.text = victory ? "胜利" : "失败";
    }

    private void RestartLevel()
    {
        LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ReturnToMenu()
    {
        LoadScene("BeginScene");
    }

    private void LoadScene(string sceneName)
    {
        // OverPanel 位于常驻 Canvas 下，切场景前必须主动移除，否则会覆盖新场景的菜单 UI。
        UIManager.Instance.HidePanel<OverPanel>(false);
        SceneManager.LoadScene(sceneName);
    }
}
