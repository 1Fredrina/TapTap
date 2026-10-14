using UnityEngine;

public class AuraVisual : MonoBehaviour
{
    private Player owner;
    private float radius;

    public void Init(Player owner, float radius)
    {
        this.owner = owner;
        this.radius = radius;
        transform.localScale = Vector3.one * radius * 2f;
    }

    private void Update()
    {
        if (owner == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = owner.transform.position;
    }
}