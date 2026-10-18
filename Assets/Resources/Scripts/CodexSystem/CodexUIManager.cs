using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Events; // ✅ 新增引用

// ✅ 改动1: 继承 BasePanel
public class CodexUIManager : BasePanel
{
    [Header("1. 数据配置")]
    public List<EnemyCodexData> allEnemies;

    [Header("2. 列表区 (左侧)")]
    public Transform listParent;
    public GameObject itemPrefab;

    [Header("3. 详情区 (右侧)")]
    public Image detailIcon;
    public TextMeshProUGUI detailName;
    public TextMeshProUGUI detailDesc;
    public TextMeshProUGUI detailKills;
    public GameObject lockPanel;

    // ✅ 改动2: 实现抽象方法 Init()
    // 原 Start() 中的 BuildList() 移到这里
    // Init 只在面板首次 Instantiate 时调用一次（用于绑定事件等）
    public override void Init()
    {
        // 如果返回按钮是在代码中动态绑定的，在这里做
        // 如果是 Prefab 里拖拽绑定的 OnBackClick，这里可以留空或只做初始化校验
        Debug.Log("[Codex] 📖 图鉴面板 Init 完成");
    }

    // ✅ 改动3: 重写 ShowMe，每次打开都刷新数据 + 触发淡入
    public override void ShowMe()
    {
        base.ShowMe(); // ⚠️ 必须先调 base，设置 isShow=true 启动淡入动画
        BuildList();   // 每次显示都重新构建列表（获取最新解锁/击杀状态）
    }

    // ✅ 改动4: 重写 HideMe，触发淡出并在动画结束后执行销毁回调
    public override void HideMe(UnityAction callback)
    {
        base.HideMe(callback); // ⚠️ 必须先调 base，设置 isShow=false 启动淡出动画
        // 不要在这里手动 Destroy 或 SetActive(false)！
        // UIManager 会在淡出完成后通过 callback 自动处理
    }

    // ✅ 改动5: 返回按钮回调
    public void OnBackClick()
    {
        UIManager.Instance.HidePanel<CodexUIManager>();
    }

    // ========== 以下原有代码完全不动 ==========
    private void BuildList()
    {
        foreach (Transform child in listParent) Destroy(child.gameObject);

        foreach (var data in allEnemies)
        {
            bool unlocked = CodexSaveManager.IsUnlocked(data.id);
            GameObject go = Instantiate(itemPrefab, listParent);

            var nameText = go.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null)
                nameText.text = unlocked ? data.displayName : "???";

            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() =>
                {
                    Debug.Log($"[Codex] 🖱️ 列表项被点击: {data.displayName} | ID: {data.id}");
                    ShowDetail(data, unlocked);
                });
            }
            else
            {
                Debug.LogError($"[Codex] ❌ 列表项缺少Button组件: {data.displayName}");
            }
        }

        if (allEnemies != null && allEnemies.Count > 0)
        {
            bool firstUnlocked = CodexSaveManager.IsUnlocked(allEnemies[0].id);
            ShowDetail(allEnemies[0], firstUnlocked);
        }
    }

    private void ShowDetail(EnemyCodexData data, bool unlocked)
    {
        Debug.Log($"[Codex] 📄 ShowDetail执行: {data.displayName} | 解锁状态: {unlocked}");

        if (lockPanel != null) lockPanel.SetActive(!unlocked);

        bool showContent = unlocked;
        if (detailIcon != null) detailIcon.gameObject.SetActive(showContent);
        if (detailName != null) detailName.gameObject.SetActive(showContent);
        if (detailDesc != null) detailDesc.gameObject.SetActive(showContent);
        if (detailKills != null) detailKills.gameObject.SetActive(showContent);

        if (showContent)
        {
            if (detailIcon != null) detailIcon.sprite = data.icon;
            if (detailName != null) detailName.text = data.displayName;
            if (detailDesc != null) detailDesc.text = data.desc;
            if (detailKills != null) detailKills.text = $"击杀数: {CodexSaveManager.GetKillCount(data.id)}";
        }
    }
}