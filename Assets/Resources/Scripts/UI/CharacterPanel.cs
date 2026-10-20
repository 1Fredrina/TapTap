using UnityEngine;
using UnityEngine.UI;

public class CharacterPanel : BasePanel
{
    public Button btnExit;
    public Button btnC1;
    public Button btnC2;
    public Button btnC3;
    public Button btnC4;
    public Text txtName;
    public Text txtIntro;
    public Button btnSkill1;
    public Button btnSkill2;
    public Button btnSkill3;
    public Button btnSkill4;
    public GameObject[] gameObjects;

    public Button btnSelect;

    private int selectedId = 1;

    private void OnEnable()
    {
        AudioManager.EnsureInstance().BeginCharacterSelection(this);
    }

    public override void ShowMe()
    {
        base.ShowMe();
        AudioManager.EnsureInstance().BeginCharacterSelection(this);
    }

    public override void HideMe(UnityEngine.Events.UnityAction callback)
    {
        AudioManager.Instance?.EndCharacterSelection(this);
        base.HideMe(callback);
    }

    private void OnDisable()
    {
        AudioManager.Instance?.EndCharacterSelection(this);
    }

    public override void Init()
    {
        btnExit.onClick.AddListener(() =>
        {
            UIManager.Instance.HidePanel<CharacterPanel>();
            UIManager.Instance.ShowPanel<PreparePanel>();
        });

        btnC1.onClick.AddListener(() =>
        {
            selectedId = 1;
            txtName.text = "细胞毒性T细胞";
            txtIntro.text = "战斗力很很强的杀手，特别爱耍帅，战斗多了会变弱需要休息，但下次上场照样嚣张。";
            //imgCharacter.sprite = Resources.Load<Sprite>("Characters/Character1");
            btnSkill1.gameObject.SetActive(true);
            btnSkill2.gameObject.SetActive(false);
            btnSkill3.gameObject.SetActive(false);
            btnSkill4.gameObject.SetActive(false);
            gameObjects[0].SetActive(true);
            gameObjects[1].SetActive(false);
            gameObjects[2].SetActive(false);
            gameObjects[3].SetActive(false);
        });

        btnC2.onClick.AddListener(() =>
        {
            selectedId = 2;
            txtName.text = "树突状细胞";
            txtIntro.text = "免疫系统的小情报员，到处捡抗原碎片，可以激活其他免疫细胞。但她太兴奋时，也可能会把免疫反应搞大";
            //imgCharacter.sprite = Resources.Load<Sprite>("Characters/Character2");
            btnSkill1.gameObject.SetActive(false);
            btnSkill2.gameObject.SetActive(true);
            btnSkill3.gameObject.SetActive(false);
            btnSkill4.gameObject.SetActive(false);
            gameObjects[0].SetActive(false);
            gameObjects[1].SetActive(true);
            gameObjects[2].SetActive(false);
            gameObjects[3].SetActive(false);
        });

        btnC3.onClick.AddListener(() =>
        {
            selectedId = 3;
            txtName.text = "辅助T细胞";
            txtIntro.text = "知识渊博的傲娇话痨，担任队内指挥官，做事追求完美与万无一失。无法忍受自己的失败，如果出现失误会极力补救。看似队内最可靠之人实则精神状态堪忧。焦虑时会扶着眼镜翻书寻找解决办法";
            //imgCharacter.sprite = Resources.Load<Sprite>("Characters/Character3");
            btnSkill1.gameObject.SetActive(false);
            btnSkill2.gameObject.SetActive(false);
            btnSkill3.gameObject.SetActive(true);
            btnSkill4.gameObject.SetActive(false);
            gameObjects[0].SetActive(false);
            gameObjects[1].SetActive(false);
            gameObjects[2].SetActive(true);
            gameObjects[3].SetActive(false);
        });

        btnC4.onClick.AddListener(() =>
        {
            selectedId = 4;
            txtName.text = "巨噬细胞";
            txtIntro.text = "寡言沉稳，看起来不好惹但实际上是团队男妈妈，会默默关心队内所有成员的状态，大家也会向其倾诉心里话.";
            //imgCharacter.sprite = Resources.Load<Sprite>("Characters/Character4");
            btnSkill1.gameObject.SetActive(false);
            btnSkill2.gameObject.SetActive(false);
            btnSkill3.gameObject.SetActive(false);
            btnSkill4.gameObject.SetActive(true);
            gameObjects[0].SetActive(false);
            gameObjects[1].SetActive(false);
            gameObjects[2].SetActive(false);
            gameObjects[3].SetActive(true);
        });

        btnSkill1.onClick.AddListener(() =>
        {
            UIManager.Instance.ShowPanel<SkillPanel>();
            UIManager.Instance.GetPanel<SkillPanel>().txtSkill.text = "开启技能后短时间内攻击力变为2倍。";
        });

        btnSkill2.onClick.AddListener(() =>
        {
            UIManager.Instance.ShowPanel<SkillPanel>();
            UIManager.Instance.GetPanel<SkillPanel>().txtSkill.text = "开启技能后短时间内移速，攻速上升。";
        });

        btnSkill3.onClick.AddListener(() =>
        {
            UIManager.Instance.ShowPanel<SkillPanel>();
            UIManager.Instance.GetPanel<SkillPanel>().txtSkill.text = "开启技能后获得随时间流失的临时生命。";
        });

        btnSkill4.onClick.AddListener(() =>
        {
            UIManager.Instance.ShowPanel<SkillPanel>();
            UIManager.Instance.GetPanel<SkillPanel>().txtSkill.text = "开启技能后内晕眩范围内敌人";
        });

        btnSelect.onClick.AddListener(() =>
        {
            GameDataMgr.Instance.selectedCharacterId = selectedId;
            UIManager.Instance.GetPanel<PreparePanel>()?.RefreshCharacterPortrait();
        });

        switch (GameDataMgr.Instance != null ? GameDataMgr.Instance.selectedCharacterId : 1)
        {
            case 2: btnC2.onClick.Invoke(); break;
            case 3: btnC3.onClick.Invoke(); break;
            case 4: btnC4.onClick.Invoke(); break;
            default: btnC1.onClick.Invoke(); break;
        }
    }
}
