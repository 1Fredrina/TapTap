using UnityEngine;

public class Attack : MonoBehaviour
{
    [SerializeField] private int baseDamage = 1;


    private int currentDamage = 1;


    private void OnTriggerEnter2D(Collider2D other)
    {
        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null) return;

        enemy.TakeDamage(currentDamage);
        Destroy(gameObject);
    }
}
