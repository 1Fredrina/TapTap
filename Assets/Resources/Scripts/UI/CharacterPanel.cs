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
    public Image imgCharacter;
    public Button btnSkill1;
    public Button btnSkill2;
    public Button btnSkill3;
    public Button btnSkill4;

    public override void Init()
    {
        btnExit.onClick.AddListener(() => { 
            UIManager.Instance.HidePanel<CharacterPanel>();
            UIManager.Instance.ShowPanel<PreparePanel>();
         });
        btnC1.onClick.AddListener(() => { 
            txtName.text = "Character 1";
            txtIntro.text = "This is the introduction for Character 1.";
            imgCharacter.sprite = Resources.Load<Sprite>("Characters/Character1");
            btnSkill1.gameObject.SetActive(true);
            btnSkill2.gameObject.SetActive(false);
            btnSkill3.gameObject.SetActive(false);
            btnSkill4.gameObject.SetActive(false);
         });
        btnC2.onClick.AddListener(() => { 
            txtName.text = "Character 2";
            txtIntro.text = "This is the introduction for Character 2.";
            imgCharacter.sprite = Resources.Load<Sprite>("Characters/Character2");
            btnSkill1.gameObject.SetActive(false);
            btnSkill2.gameObject.SetActive(true);
            btnSkill3.gameObject.SetActive(false);
            btnSkill4.gameObject.SetActive(false);
         });
        btnC3.onClick.AddListener(() => { 
            txtName.text = "Character 3";
            txtIntro.text = "This is the introduction for Character 3.";
            imgCharacter.sprite = Resources.Load<Sprite>("Characters/Character3");
            btnSkill1.gameObject.SetActive(false);
            btnSkill2.gameObject.SetActive(false);
            btnSkill3.gameObject.SetActive(true);
            btnSkill4.gameObject.SetActive(false);
         });
        btnC4.onClick.AddListener(() => { 
            txtName.text = "Character 4";
            txtIntro.text = "This is the introduction for Character 4.";
            imgCharacter.sprite = Resources.Load<Sprite>("Characters/Character4");
            btnSkill1.gameObject.SetActive(false);
            btnSkill2.gameObject.SetActive(false);
            btnSkill3.gameObject.SetActive(false);
            btnSkill4.gameObject.SetActive(true);
         });
        btnSkill1.onClick.AddListener(() => { 
            UIManager.Instance.ShowPanel<SkillPanel>();
            UIManager.Instance.GetPanel<SkillPanel>().txtSkill.text = "Character 1 Skill: Fireball";
         });
        btnSkill2.onClick.AddListener(() => { 
            UIManager.Instance.ShowPanel<SkillPanel>();
            UIManager.Instance.GetPanel<SkillPanel>().txtSkill.text = "Character 2 Skill: Ice Blast";
         });
        btnSkill3.onClick.AddListener(() => { 
            UIManager.Instance.ShowPanel<SkillPanel>();
            UIManager.Instance.GetPanel<SkillPanel>().txtSkill.text = "Character 3 Skill: Lightning Strike";
         });
        btnSkill4.onClick.AddListener(() => { 
            UIManager.Instance.ShowPanel<SkillPanel>();
            UIManager.Instance.GetPanel<SkillPanel>().txtSkill.text = "Character 4 Skill: Earthquake";
         });
        btnC1.onClick.Invoke();
    }

}
