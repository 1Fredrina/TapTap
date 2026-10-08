using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private PlayerData data;
    [SerializeField] private PlayerController controller;
    [SerializeField] private GameObject attackEffectPrefab;
    private float attackStartTime;

    public PlayerData Data => data;
    public PlayerController Controller => controller;

    public bool IsAttacking { get; private set; }

    private float attackCooldownTimer; //gameplay: 攻击间隔计时器，防止攻击过快

    private void Awake()
    {
        if (controller == null)
            controller = GetComponent<PlayerController>();
        if (controller != null)
            controller.Initialize(data);
    }
    private void Update()
    {
        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Time.deltaTime;
    }

    /// <summary>由 PlayerController 在检测到 J 键时调用。</summary>
    public void OnAttackInput()
    {
        if (IsAttacking && Time.time - attackStartTime > 2f)
        {
        IsAttacking = false;
        }

        if (IsAttacking) return;
        if (attackCooldownTimer > 0f) return;

        IsAttacking = true;
        attackCooldownTimer = data.AttackInterval;
        controller.PlayAttack();
    }
    public void OnAttackHit()
    {
        if (attackEffectPrefab == null) return;

        float dir = controller.FacingDirection;
        Vector3 spawnPos = transform.position + new Vector3(dir * 0.5f, 0f, 0f);
        GameObject bullet = Instantiate(attackEffectPrefab, spawnPos, Quaternion.identity);

        if (bullet.TryGetComponent<Bullet>(out var b))
            b.Init(dir);
        
    }

    public void OnAttackAnimationEnd()
    {
        IsAttacking = false;
    }

    public void TakeDamage(int dmg = 1)
    {
    if (GameManager.Instance == null) return;
    if (GameManager.Instance.IsGameOver) return;

    GameManager.Instance.LoseLife(dmg);

    // TODO 下一轮：受伤动画、无敌帧、击退
    }
}