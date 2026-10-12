using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class LevelCamera : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);//摄像机相对玩家的位置偏移

    [Header("边界限制")]
    [SerializeField] private bool clampCamera = true;//启用摄像机边界限制
    [SerializeField] private Transform minBound;
    [SerializeField] private Transform maxBound;

    void LateUpdate()//限制摄像机边界
    {
        if (player == null) return;

        Vector3 desiredPos = player.position + offset;

        if (clampCamera && minBound != null && maxBound != null)
        {
            Vector3 min = minBound.position;
            Vector3 max = maxBound.position;

            desiredPos.x = Mathf.Clamp(desiredPos.x, min.x, max.x);
            desiredPos.y = Mathf.Clamp(desiredPos.y, min.y, max.y);
        }

        transform.position = desiredPos;
    }
}