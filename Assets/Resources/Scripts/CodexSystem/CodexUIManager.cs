using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CodexUIManager : MonoBehaviour
{
    [Header("1. 数据配置")]
    public List<EnemyCodexData> allEnemies; // 在Inspector拖入所有怪物SO

    [Header("2. 列表区 (左侧)")]
    public Transform listParent;            // 列表的父节点 (需挂载 GridLayoutGroup)
    public GameObject itemPrefab;           // 列表条目预制体 (需包含 Image, TMP_Text, Button)

    [Header("3. 详情区 (右侧)")]
    public Image detailIcon;                // 详情大图
    public TextMeshProUGUI detailName;      // 详情名字
    public TextMeshProUGUI detailDesc;      // 详情描述
    public TextMeshProUGUI detailKills;     // 击杀数文本
    public GameObject lockPanel;            // 未解锁时显示的遮罩/锁图标

    private void OnEnable() => BuildList();

    private void BuildList()
    {
        // 清空旧列表
        foreach (Transform child in listParent) Destroy(child.gameObject);

        // 生成新列表
        foreach (var data in allEnemies)
        {
            bool unlocked = CodexSaveManager.IsUnlocked(data.enemyID);
            GameObject go = Instantiate(itemPrefab, listParent);

            // 设置列表条目显示
            go.GetComponentInChildren<TextMeshProUGUI>().text = unlocked ? data.displayName : "???";
            var img = go.GetComponentInChildren<Image>();
            if (img != null) img.sprite = unlocked ? data.icon : null;

            // 绑定点击事件
            go.GetComponent<Button>().onClick.AddListener(() => ShowDetail(data, unlocked));
        }
    }

    private void ShowDetail(EnemyCodexData data, bool unlocked)
    {
        // 切换未解锁/已解锁面板状态
        if (lockPanel != null) lockPanel.SetActive(!unlocked);
        if (detailIcon != null) detailIcon.gameObject.SetActive(unlocked);
        if (detailName != null) detailName.gameObject.SetActive(unlocked);
        if (detailDesc != null) detailDesc.gameObject.SetActive(unlocked);
        if (detailKills != null) detailKills.gameObject.SetActive(unlocked);

        if (unlocked)
        {
            if (detailIcon != null) detailIcon.sprite = data.icon;
            if (detailName != null) detailName.text = data.displayName;
            if (detailDesc != null) detailDesc.text = data.desc;
            if (detailKills != null) detailKills.text = $"击杀数: {CodexSaveManager.GetKillCount(data.enemyID)}";
        }
    }
}