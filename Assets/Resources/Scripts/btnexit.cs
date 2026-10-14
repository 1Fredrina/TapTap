using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class btnexit : MonoBehaviour
{
    public Button button;

    void Start()
    {
        button.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("BeginScene");
            UIManager.Instance.ShowPanel<PreparePanel>();
        });
    }
}
