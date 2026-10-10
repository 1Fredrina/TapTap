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

    private GameDataMgr()
    {
        musicData = JsonMgr.Instance.LoadData<MusicData>("MusicData");
        playerDatas = JsonMgr.Instance.LoadData<List<PlayerData>>("PlayerData");
    }
}