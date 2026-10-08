using UnityEngine;


// Bullet.cs
public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    private float direction = 1f;

    public void Init(float dir)
    {
        direction = dir;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * dir;
        transform.localScale = scale;
    }

    private void Update()
    {
        transform.Translate(Vector3.right * direction * speed * Time.deltaTime);
    }
}
