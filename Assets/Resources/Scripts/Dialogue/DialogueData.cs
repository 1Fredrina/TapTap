using UnityEngine;

// 一条剧情台词的数据结构
// 每一句台词 = 说话角色名 + 台词文字 + 这句要显示的立绘
[System.Serializable]
public struct DialogueLine
{
    // 说话角色名（显示在对话框上方的名字，无名字可留空）
    [SerializeField] private string speakerName;

    // 这句台词的具体文字内容（打字机效果逐字显示的就是它）
    [SerializeField] private string content;

    // 这句台词对应的角色立绘（留空则该句不显示立绘）
    [SerializeField] private Sprite portrait;

    // 下面三个只读属性：让外部只读不改，保证数据不会被意外篡改
    public string SpeakerName => speakerName;
    public string Content => content;
    public Sprite Portrait => portrait;
}

// 整段剧情：把所有台词按顺序放进一个数组
// 在 Unity 里用 右键 -> Create -> 剧情 -> 开局剧情 来创建这个资产，然后逐条填数据
[CreateAssetMenu(fileName = "NewDialogue", menuName = "剧情/开局剧情")]
public class DialogueData : ScriptableObject
{
    // 剧情台词列表，按数组下标 0、1、2…… 的先后顺序播放
    [SerializeField] private DialogueLine[] lines;

    // 台词总条数（播放时用来判断是否播完）
    public int LineCount => lines == null ? 0 : lines.Length;

    // 按下标取出某一条台词
    public DialogueLine GetLine(int index) => lines[index];
}