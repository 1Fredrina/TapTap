using System.Collections.Generic;
using UnityEngine;

public class LevelsMgr : MonoBehaviour
{
    private static LevelsMgr instance;
    public static LevelsMgr Instance
    {
        get
        {
            if (instance == null)
                instance = new LevelsMgr();
            return instance;
        }
    }
    public List<LevelsData> levelsDatas;
    private LevelsMgr()
    {
        levelsDatas = JsonMgr.Instance.LoadData<List<LevelsData>>("LevelsData");
    }
}
