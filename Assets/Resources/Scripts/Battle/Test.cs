using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    [SerializeField] private int damage = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<Player>(out var player))
            player.TakeDamage(damage);
    }
}