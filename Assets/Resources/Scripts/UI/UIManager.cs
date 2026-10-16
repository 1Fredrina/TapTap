using System.Collections.Generic;
using UnityEngine;

public class UIManager
{
    private static UIManager instance=new UIManager();

    public static UIManager Instance=> instance;
    private Transform canvasTrans;


    //用于存储显示的面板
    private Dictionary<string, BasePanel> panelDic = new Dictionary<string, BasePanel>();

    private UIManager()
    {
        GameObject canvas=GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/UI/Canvas"));
        canvasTrans = canvas.transform;
        GameObject.DontDestroyOnLoad(canvas);
    }

    public T ShowPanel<T>() where T : BasePanel
    {
        string panelName = typeof(T).Name;
        RemoveDestroyedPanel(panelName);
        if (panelDic.ContainsKey(panelName))
        {
            T existingPanel = panelDic[panelName] as T;
            existingPanel?.ShowMe();
            return existingPanel;
        }
        GameObject panelObj = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/UI/" + panelName));
        panelObj.transform.SetParent(canvasTrans, false);
        T panel = panelObj.GetComponent<T>();
        panelDic.Add(panelName, panel);
        panel.ShowMe();
        return panel;
    }

    public void HidePanel<T>(bool isFade=true) where T : BasePanel
    {
        string panelName = typeof(T).Name;
        RemoveDestroyedPanel(panelName);
        if (panelDic.ContainsKey(panelName))
        {
            if (isFade)
            {
                panelDic[panelName].HideMe(() =>
                {
                    GameObject.Destroy(panelDic[panelName].gameObject);
                    panelDic.Remove(panelName);
                });
            }
            else
            {
                GameObject.Destroy(panelDic[panelName].gameObject);
                panelDic.Remove(panelName);
            }
            
        }
    }

    public T GetPanel<T>() where T : BasePanel
    {
        string panelName = typeof(T).Name;
        RemoveDestroyedPanel(panelName);
        if (panelDic.ContainsKey(panelName))
        {
            return panelDic[panelName] as T;
        }
        return null;
    }

    private void RemoveDestroyedPanel(string panelName)
    {
        // 退出关卡或播放模式时，Unity 可能先于 GameManager 销毁面板。
        if (panelDic.TryGetValue(panelName, out BasePanel panel) && panel == null)
            panelDic.Remove(panelName);
    }
    
}
