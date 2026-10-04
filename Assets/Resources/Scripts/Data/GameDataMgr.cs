using System.Collections.Generic;
using UnityEngine;

public class GameDataMgr
{
    private static GameDataMgr instance=new GameDataMgr();
    public static GameDataMgr Instance=> instance;
    public MusicData musicData;
    
    public List<PlayerData> playerDatas;
    private GameDataMgr()
    {
        musicData=JsonMgr.Instance.LoadData<MusicData>("MusicData");
        playerDatas=JsonMgr.Instance.LoadData<List<PlayerData>>("PlayerData");
    }
}
