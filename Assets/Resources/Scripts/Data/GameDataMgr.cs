using System.Collections.Generic;
using UnityEngine;

public class GameDataMgr
{
    private static GameDataMgr instance=new GameDataMgr();
    public static GameDataMgr Instance=> instance;
    public MusicData musicData;
    //记录选择的角色
    private GameDataMgr()
    {
        musicData=JsonMgr.Instance.LoadData<MusicData>("MusicData");
    }
}
