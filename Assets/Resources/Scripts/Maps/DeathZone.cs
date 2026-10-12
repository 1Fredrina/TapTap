using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathZone : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 重新加载当前关卡
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}