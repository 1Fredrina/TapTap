using UnityEngine;

public class Enemy : MonoBehaviour
{
    public void Stun(float duration)
    {
        Debug.Log($"[Enemy] Stunned for {duration}s");
        // TODO: 实际晕眩逻辑
    }
}