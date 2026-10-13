using UnityEngine;

public class Shockwave : MonoBehaviour
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifeTime = 1.5f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float moveDirection = 1f;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.Translate(Vector3.right * moveDirection * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Player player = other.GetComponentInParent<Player>();
        if (player == null) return;

        player.TakeDamage(damage, transform.position.x);
    }
    public void SetDirection(float dir)
    {
    moveDirection = dir;

    Vector3 scale = transform.localScale;
    scale.x = Mathf.Abs(scale.x) * dir;
    transform.localScale = scale;
    }
}