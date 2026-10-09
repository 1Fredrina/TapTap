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
    public bool IsInvincible => invincibleTimer > 0f;
    public SkillBase Skill => skill;


    private SkillBase skill;
    private DamageBuffSkill damageBuff;
    private DoubleActionSkill doubleAction;
    private float skillCooldownTimer;


    private float attackCooldownTimer; //gameplay: 攻击间隔计时器，防止攻击过快
    private SpriteRenderer[] allRenderers;//角色无敌相关
    private float invincibleTimer;
    private float blinkTimer;
    private bool[] initialEnabledStates;

    public bool DoubleActionActive => doubleAction != null && doubleAction.IsActive;

    public void Initialize(PlayerData d)
    {
        data = d;

        if (data != null && !string.IsNullOrEmpty(data.skillId))
        {
            skill = SkillFactory.Create(data.skillId);
            if (skill != null && data.skillCooldown > 0f)
            skill.Cooldown = data.skillCooldown;
            damageBuff = skill as DamageBuffSkill;
            doubleAction = skill as DoubleActionSkill;
        }
    }

    private void Awake()
    {
        if (controller == null)
            controller = GetComponent<PlayerController>();
        if (controller != null)
            controller.Initialize(data);
        if (data != null && !string.IsNullOrEmpty(data.skillId)){
            skill = SkillFactory.Create(data.skillId);}
        allRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        initialEnabledStates = new bool[allRenderers.Length];
        for (int i = 0; i < allRenderers.Length; i++)
            initialEnabledStates[i] = allRenderers[i].enabled;
    }
    private void OnDisable()
    {
        // 切人会中断动画，不能依赖 OnAttackAnimationEnd 清理攻击状态。
        CancelAttack();
    }

    private void Update()
    {
        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Time.deltaTime;
        if (skillCooldownTimer > 0f)
            skillCooldownTimer -= Time.deltaTime;
        UpdateInvincible();
        if (damageBuff != null)
            damageBuff.Tick(Time.deltaTime);
        if (doubleAction != null) doubleAction.Tick(Time.deltaTime);
    }

    public void OnSkillInput()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        if (controller.IsStunned) return;
        if (skill == null) return;
        if (skillCooldownTimer > 0f) return;

        skillCooldownTimer = skill.Cooldown;
        skill.Cast(this);
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
        b.Init(dir, DamageMultiplier);
        
    }

    public void OnAttackAnimationEnd()
    {
        IsAttacking = false;
    }

    public void TakeDamage(int dmg = 1,float sourceX = float.NaN)
    {
    if (GameManager.Instance == null) return;
    if (GameManager.Instance.IsGameOver) return;
    if (IsInvincible) return;

    if (float.IsNaN(sourceX))
        sourceX = transform.position.x + controller.FacingDirection;

    controller.ApplyKnockback(sourceX);

    GameManager.Instance.LoseLife(dmg);
    StartInvincible();//开始无敌
    }

    private void StartInvincible()
    {
        invincibleTimer = data.InvincibleTime;
        blinkTimer = 0f;
    }

    private void UpdateInvincible()
    {
        if (invincibleTimer > 0f)
        {
            invincibleTimer -= Time.deltaTime;

            blinkTimer += Time.deltaTime;
            if (blinkTimer >= 0.1f)
            {
                blinkTimer = 0f;
                ToggleRenderers();
            }

            if (invincibleTimer <= 0f)
                SetRenderersVisible(true);
        }
    }

    private void ToggleRenderers()
    {
        if (allRenderers == null) return;
        bool visible = !allRenderers[0].enabled;
        SetRenderersVisible(visible);
    }

    private void SetRenderersVisible(bool visible)
    {
        if (allRenderers == null) return;
        foreach (var sr in allRenderers)
        {
            if (sr != null)
                sr.enabled = visible;
        }
    }

    public float DamageMultiplier
    {
        get
        {
            if (damageBuff != null && damageBuff.IsActive)
                return damageBuff.Multiplier;
            return 1f;
        }
    }

    public float DashCooldownMultiplier
    {
        get
        {
            if (doubleAction != null && doubleAction.IsActive)
                return doubleAction.DashCooldownMultiplier;
            return 1f;
        }
    }

    public void CancelAttack()
    {
        IsAttacking = false;
    }

}
