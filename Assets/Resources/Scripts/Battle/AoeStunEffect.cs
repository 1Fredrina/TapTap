using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class AoeStunEffect : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.5f;

    private float stunDuration = 2f;

    public void Init(float stun)
    {
        stunDuration = stun;
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 碰到敌人就让它晕
        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            enemy.Stun(stunDuration);
        }
    }
}
