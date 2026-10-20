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

    private bool initialized;

    public override void Init()
    {
        if (!initialized)
        {
            initialized = true;
            if (btnLevels != null)
                for (int i = 0; i < btnLevels.Length; i++)
                {
                    int levelId = i + 1;
                    if (btnLevels[i] != null)
                        btnLevels[i].onClick.AddListener(() => LoadLevel(levelId));
                }

            if (btnC1 != null)
                btnC1.onClick.AddListener(() => { L1.SetActive(true); L2.SetActive(false); });
            if (btnExit != null)
                btnExit.onClick.AddListener(() =>
                {
                    UIManager.Instance.HidePanel<LevelPanel>();
                    UIManager.Instance.ShowPanel<PreparePanel>();
                });
        }

        RefreshUnlockState();
    }

    public override void ShowMe()
    {
        base.ShowMe();
        Init();
    }

    public void RefreshUnlockState()
    {
        if (btnLevels == null) return;
        for (int i = 0; i < btnLevels.Length; i++)
        {
            Button btn = btnLevels[i];
            if (btn == null) continue;
            int levelId = i + 1;
            bool unlocked = LevelsMgr.Instance.IsLevelUnlocked(levelId) &&
                            Application.CanStreamedLevelBeLoaded("Level" + levelId);
            btn.interactable = unlocked;
            Image img = btn.GetComponent<Image>();
            if (img != null)
                img.color = unlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
        }
    }

    private void LoadLevel(int levelId)
    {
        if (!LevelsMgr.Instance.IsLevelUnlocked(levelId) ||
            !Application.CanStreamedLevelBeLoaded("Level" + levelId)) return;

        SceneManager.LoadScene("Level" + levelId);
        UIManager.Instance.HidePanel<LevelPanel>();
    }
}
