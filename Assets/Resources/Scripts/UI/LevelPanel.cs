using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelPanel : BasePanel
{
    public Button btnC1;
    public Button btnC2;
    public Button btnExit;
    public GameObject L1;
    public GameObject L2;
    public Button[] btnLevels;

    public override void Init()
    {
        for (int i = 0; i < btnLevels.Length; i++)
        {
            int index = i;
            Button btn = btnLevels[i];

            int levelId = i + 1;
            bool unlocked = IsLevelUnlocked(levelId);

            btn.interactable = unlocked;
            Image img = btn.GetComponent<Image>();
            if (img != null)
                img.color = unlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);

            if (unlocked)
            {
                btn.onClick.AddListener(() =>
                {
                    SceneManager.LoadScene("Level" + levelId);
                    UIManager.Instance.HidePanel<LevelPanel>();
                });
            }
        }

        btnC1.onClick.AddListener(() => { L1.SetActive(true); L2.SetActive(false); });
        btnExit.onClick.AddListener(() =>
        {
            UIManager.Instance.HidePanel<LevelPanel>();
            UIManager.Instance.ShowPanel<PreparePanel>();
        });
    }

    private bool IsLevelUnlocked(int levelId)
    {
        var list = LevelsMgr.Instance.levelsDatas;
        if (list == null) return false;

        foreach (var data in list)
        {
            if (data.LevelsID == levelId)
                return data.IsUnlock;
        }
        return false;
    }
}