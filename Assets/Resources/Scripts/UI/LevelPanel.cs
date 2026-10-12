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
        for(int i = 0; i < btnLevels.Length; i++)
        {
            int index = i;
            btnLevels[i].onClick.AddListener(() => { 
                //SceneManager.LoadScene("Level" + (index + 1).ToString());
                Debug.Log("Load Level " + (index + 1).ToString());
             });
        }
        btnC1.onClick.AddListener(() => { L1.SetActive(true); L2.SetActive(false); });
        btnC2.onClick.AddListener(() => { L1.SetActive(false); L2.SetActive(true); });
        btnExit.onClick.AddListener(() => { 
            UIManager.Instance.HidePanel<LevelPanel>();
            UIManager.Instance.ShowPanel<PreparePanel>();
         });
    }

}
