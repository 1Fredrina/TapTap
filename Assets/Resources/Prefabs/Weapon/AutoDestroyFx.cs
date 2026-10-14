using UnityEngine;

public class AutoDestroyFx : MonoBehaviour
{
    [Tooltip("秒数，建议略大于 Trail 的 Time")]
    public float lifetime = 0.25f;

    void Start() => Destroy(gameObject, lifetime);
}