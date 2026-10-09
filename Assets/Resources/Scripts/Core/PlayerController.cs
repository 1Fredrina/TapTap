using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerData playerData = new PlayerData();
    [SerializeField] private LayerMask groundLayers = Physics2D.DefaultRaycastLayers;
    [SerializeField, Min(0f)] private float dashSpeed = 16f;
    [SerializeField, Min(0.02f)] private float dashDuration = 0.15f;

    private Rigidbody2D body;
    private Animator animator;
    private Player player;
    private ContactFilter2D groundFilter;

    private int jumpCount;
    private float moveInput;
    private float facingDirection = 1f;
    private float dashTime;
    private float dashCooldownTimer;
    private float stunTimer;
    private bool jumpRequested;
    private bool dashRequested;
    private bool wasGrounded;

    public float FacingDirection => facingDirection;
    public bool IsStunned => stunTimer > 0f;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        player = GetComponent<Player>();
        // 攻击会修改子节点的姿态和显隐，停用时还原，避免切回后将攻击帧作为默认值。
        if (animator != null)
            animator.writeDefaultValuesOnDisable = true;
        body.constraints |= RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        groundFilter.SetLayerMask(groundLayers);
        groundFilter.SetNormalAngle(45f, 135f);
        groundFilter.useTriggers = false;
    }

    private void Update()
    {
        if (IsStunned)
        {
            moveInput = 0f;
            jumpRequested = false;
            dashRequested = false;
            return;
        }

        moveInput = Input.GetAxisRaw("Horizontal");
        jumpRequested |= Input.GetKeyDown(KeyCode.Space);
        dashRequested |= Input.GetKeyDown(KeyCode.LeftShift);

        if (Input.GetKeyDown(KeyCode.J) && player != null)
            player.OnAttackInput();

        if (Input.GetKeyDown(KeyCode.K) && player != null)
            player.OnSkillInput();
    }

    public void Initialize(PlayerData data)
    {
        playerData = data;
    }

    private void FixedUpdate()
    {
        bool grounded = body.IsTouching(groundFilter);

        if (grounded && body.linearVelocity.y <= 0.05f && !wasGrounded)
        {
            jumpCount = 0;
            dashCooldownTimer = 0f;
        }
        wasGrounded = grounded;

        // 硬直处理：保留击退惯性，不响应输入
        if (IsStunned)
        {
            stunTimer -= Time.fixedDeltaTime;

            Vector2 stunnedVel = body.linearVelocity;
            stunnedVel.x = Mathf.MoveTowards(stunnedVel.x, 0f, 20f * Time.fixedDeltaTime);
            body.linearVelocity = stunnedVel;

            UpdateAnimationParams();
            return;
        }

        bool doubleActive = player != null && player.DoubleActionActive;
        int maxJump = doubleActive ? 2 : 1;

        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.fixedDeltaTime;

        // 朝向
        if (dashTime <= 0f && moveInput != 0f)
        {
            facingDirection = Mathf.Sign(moveInput);
            ApplyFacing();
        }

        // 冲刺
        if (dashRequested && dashTime <= 0f && dashCooldownTimer <= 0f)
        {
            dashTime = Mathf.Max(dashDuration, Time.fixedDeltaTime);

            float multiplier = player != null ? player.DashCooldownMultiplier : 1f;
            dashCooldownTimer = playerData.DashCooldown * multiplier;

            if (animator != null)
                animator.SetTrigger("IsDash");
        }
        dashRequested = false;

        // 水平速度
        Vector2 velocity = body.linearVelocity;
        velocity.x = dashTime > 0f
            ? facingDirection * dashSpeed
            : moveInput * playerData.MoveSpeed;

        // 跳跃
        if (jumpRequested && jumpCount < maxJump)
        {
            bool canJump = grounded || jumpCount > 0;
            if (canJump && velocity.y <= 0.5f)
            {
                float gravity = -Physics2D.gravity.y * body.gravityScale;
                if (gravity > 0f && playerData.JumpHeight > 0f)
                {
                    velocity.y = Mathf.Sqrt(2f * gravity * playerData.JumpHeight);
                    jumpCount++;
                    if (animator != null)
                        animator.SetTrigger("IsJump");
                }
            }
        }
        jumpRequested = false;

        body.linearVelocity = velocity;
        UpdateAnimationParams();
        dashTime = Mathf.Max(0f, dashTime - Time.fixedDeltaTime);
    }

    public void ApplyKnockback(float sourceX)
    {
        stunTimer = playerData.StunTime;

        float dir = transform.position.x < sourceX ? -1f : 1f;
        body.linearVelocity = new Vector2(dir * playerData.KnockbackForce, playerData.KnockbackUpForce);
    }

    private void OnDisable()
    {
        moveInput = 0f;
        dashTime = 0f;
        dashCooldownTimer = 0f;
        stunTimer = 0f;
        jumpRequested = false;
        dashRequested = false;
        wasGrounded = false;
        if (body != null)
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
    }

    public void PlayAttack()
    {
        if (animator != null)
            animator.SetTrigger("IsAtk");
    }

    private void UpdateAnimationParams()
    {
        if (animator == null)
            return;
        animator.SetBool("IsRun", Mathf.Abs(moveInput) > 0.01f);
    }

    private void ApplyFacing()
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * facingDirection;
        transform.localScale = scale;
    }


}
