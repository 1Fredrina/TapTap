using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

// 剧情播放控制器：把 DialogueData 里的台词按顺序一条条放出来。
// 负责三件事：1) 切换立绘  2) 打字机逐字显示台词  3) 点击/按键推进。
// 全部播完后通过 finishedEvent 通知外部（用于关闭剧情、进入主游戏）。
public class DialoguePlayer : MonoBehaviour
{
    [Header("数据")]
    // 要播放的那段剧情资产（第一步建的 ScriptableObject，在 Inspector 里拖进来）
    [SerializeField] private DialogueData data;

    [Header("界面引用（都在 Inspector 里拖进来）")]
    [SerializeField] private CanvasGroup rootGroup;  // 整个剧情界面的根，用来控制显隐
    [SerializeField] private TMP_Text speakerText;   // 显示角色名
    [SerializeField] private TMP_Text contentText;   // 显示台词正文（打字机作用在它身上）
    [SerializeField] private Image portraitImage;    // 显示立绘

    [Header("播放设置")]
    [SerializeField, Min(0f)] private float charInterval = 0.03f; // 每个字之间的间隔（秒）

    [Header("播完后的动作（在 Inspector 里绑定）")]
    // 剧情放完时触发，可以绑"关闭剧情画面""加载主场景"等
    [SerializeField] private UnityEvent finishedEvent;

    private int currentIndex = 0;        // 当前播放到第几条台词
    private bool isTyping = false;       // 打字机是否还在逐字显示
    private bool forceFullText = false;  // 是否要求立刻显示整句（打字中点一下=跳过打字）
    private Coroutine typingRoutine = null; // 打字机协程的句柄，方便随时停掉

    // 游戏启动即自动开始播第一条
    void Start()
    {
        PlayLine(0);
    }

    // 每帧检测点击/按键，决定是"跳过打字"还是"切下一句"
    void Update()
    {
        bool clicked = Input.GetMouseButtonDown(0)

                       || Input.GetKeyDown(KeyCode.Return)
                       || Input.GetKeyDown(KeyCode.Space);
        if (!clicked) return;

        if (isTyping)
        {
            forceFullText = true; // 正在打字：点一下 = 立刻把整句显示完
        }
        else
        {
            NextLine();           // 已经显示完整句：点一下 = 进入下一条
        }
    }

    // 播放指定下标的台词：设置角色名 + 立绘 + 启动打字机
    void PlayLine(int index)
    {
        currentIndex = index;
        DialogueLine line = data.GetLine(index);

        // 角色名（没有名字就显示空）
        speakerText.text = line.SpeakerName;

        // 立绘：这条有立绘就显示，没有就隐藏（实现立绘切换/隐藏）
        if (line.Portrait != null)
        {
            portraitImage.sprite = line.Portrait;
            portraitImage.enabled = true;
        }
        else
        {
            portraitImage.enabled = false;
        }

        // 启动打字机
        contentText.text = "";
        forceFullText = false;
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = StartCoroutine(Typewriter(line.Content));
    }

    // 打字机协程：一个字一个字往外蹦
    IEnumerator Typewriter(string fullText)
    {
        isTyping = true;
        int shown = 0;

        while (shown < fullText.Length)
        {
            // 如果玩家点击要求跳过，直接把整句显示出来
            if (forceFullText)
            {
                contentText.text = fullText;
                shown = fullText.Length;
                break;
            }

            shown++;
            contentText.text = fullText.Substring(0, shown);
            yield return new WaitForSeconds(charInterval);
        }

        // 兜底：确保最终显示的是完整文本
        contentText.text = fullText;
        isTyping = false;
        typingRoutine = null;
    }

    // 切到下一条；如果已经是最后一条，则播放结束
    void NextLine()
    {
        if (currentIndex + 1 >= data.LineCount)
        {
            Finish();
            return;
        }
        PlayLine(currentIndex + 1);
    }

    // 剧情全部播完：关掉剧情画面，并触发外部动作
    void Finish()
    {
        rootGroup.alpha = 0f;
        rootGroup.interactable = false;
        rootGroup.blocksRaycasts = false;
        finishedEvent.Invoke();
    }
}