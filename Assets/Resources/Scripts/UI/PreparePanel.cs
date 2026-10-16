using UnityEngine;
using UnityEngine.UI;

public class PreparePanel : BasePanel
{
    [Header("角色立绘")]
    [Tooltip("按角色 ID 顺序绑定 1～4 号角色立绘。")]
    public Image[] characterPortraits = new Image[4];

    public Button btnStart;
    public Button btnSetting;
    public Button btnAchievement;
    public Button btnCharacter;
    public Button btnBook;

    public override void Init()
    {
        RefreshCharacterPortrait();

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
            UIManager.Instance.HidePanel<PreparePanel>();
            UIManager.Instance.ShowPanel<AchievementPanel>();
        });

        btnCharacter.onClick.AddListener(() =>
        {
            UIManager.Instance.HidePanel<PreparePanel>();
            UIManager.Instance.ShowPanel<CharacterPanel>();
        });

        if (btnBook != null)
        {
            btnBook.onClick.AddListener(() =>
            {
                // 图鉴面板，需要时打开
                // UIManager.Instance.ShowPanel<BookPanel>();
            });
        }
    }

    public override void ShowMe()
    {
        base.ShowMe();
        RefreshCharacterPortrait();
    }

    public void RefreshCharacterPortrait()
    {
        if (characterPortraits == null || characterPortraits.Length == 0) return;

        int characterId = GameDataMgr.Instance != null
            ? GameDataMgr.Instance.selectedCharacterId
            : 1;

        int selectedIndex = Mathf.Clamp(characterId - 1, 0, characterPortraits.Length - 1);

        for (int i = 0; i < characterPortraits.Length; i++)
        {
            if (characterPortraits[i] == null) continue;
            characterPortraits[i].enabled = (i == selectedIndex);
        }
    }
}