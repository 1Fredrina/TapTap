using System.Collections.Generic;
using UnityEngine;

public class GameDataMgr
{
    private static GameDataMgr instance;
    public static GameDataMgr Instance
    {
        get
        {
            if (instance == null)
                instance = new GameDataMgr();
            return instance;
        }
    }

    public MusicData musicData;
    public List<PlayerData> playerDatas;
    public List<TalentBase> talentDatas;
    public List<EnemyTalentData> enemyTalentDatas;

    public int selectedCharacterId = 1;

    private GameDataMgr()
    {
        //Debug.Log("Loading MusicData");
        //musicData = JsonMgr.Instance.LoadData<MusicData>("MusicData");

        Debug.Log("Loading PlayerData");
        playerDatas = JsonMgr.Instance.LoadData<List<PlayerData>>("PlayerData");

        Debug.Log("Loading TalentData");
        var entries = JsonMgr.Instance.LoadData<List<TalentJsonEntry>>("TalentData");
        talentDatas = TalentConverter.Convert(entries);
        enemyTalentDatas = JsonMgr.Instance.LoadData<List<EnemyTalentData>>("EnemyTalentData");
    }
}
