using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("跟随设置")]
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, -10f);

    private Transform target;
    private Vector3 velocity = Vector3.zero;

    private void Start()
    {
        RefreshTarget();
    }

    private void LateUpdate()
    {
        if (target == null || !target.gameObject.activeInHierarchy)
            RefreshTarget();

        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
    }

    private void RefreshTarget()
    {
        if (GameManager.Instance == null) return;
        var team = GameManager.Instance.PlayerObject;
        if (team == null) return;
    }
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}