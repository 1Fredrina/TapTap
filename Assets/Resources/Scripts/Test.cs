using UnityEngine;

public class Test : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //这是一个测试
        UIManager.Instance.ShowPanel<BeginPanel>();
    }

}
