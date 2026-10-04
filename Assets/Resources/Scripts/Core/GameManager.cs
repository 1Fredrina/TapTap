using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance => instance;

    public List<GameObject> teamMembers = new List<GameObject>();
    [SerializeField] private int teamSize = 4;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    void Start()
    {
        //Debug.Log("GameManager Start");
        //显示游戏面板
        //初始化角色
        //Debug.Log($"playerDatas count = {GameDataMgr.Instance.playerDatas?.Count}");
        
        for (int i = 0; i < GameDataMgr.Instance.playerDatas.Count; i++)
        {
            PlayerData playerData = GameDataMgr.Instance.playerDatas[i];
            GameObject PlayerObj = Instantiate(Resources.Load<GameObject>("Prefabs/Characters/"+ (i+1)), Vector3.zero, Quaternion.identity);
            //string path = "Prefabs/Characters/" + (i + 1);
            //GameObject prefab = Resources.Load<GameObject>(path);
            //Debug.Log($"load {path}: {prefab}");

            PlayerController controller = PlayerObj.GetComponent<PlayerController>();
            controller.Initialize(playerData);

            PlayerObj.name = playerData.name;
            teamMembers.Add(PlayerObj);
            PlayerObj.SetActive(i == 0);
        }
    }
}
