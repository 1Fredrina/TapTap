using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerData playerData = new PlayerData();
    [SerializeField, Min(0f)] private float dashSpeed = 16f;
    [SerializeField, Min(0.02f)] private float dashDuration = 0.15f;

    private Rigidbody2D body;
    private Animator animator;
    private Player player;

    private Vector2 moveInputVector;
    private Vector2 lastMoveDirection = Vector2.right;
    private Vector2 dashDirection = Vector2.right;
    private float facingDirection = 1f;
    private float dashTime;
    private float dashCooldownTimer;
    private float stunTimer;
    private bool dashRequested;

    public float FacingDirection => facingDirection;
    public Vector2 MoveInput => moveInputVector;
    public bool IsStunned => stunTimer > 0f;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        player = GetComponent<Player>();
        if (animator != null)
            animator.writeDefaultValuesOnDisable = true;
        body.constraints |= RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.gravityScale = 0f;
        body.linearDamping = 0f;
    }

    private void Update()
    {
        if (IsStunned)
        {
            moveInputVector = Vector2.zero;
            dashRequested = false;
            return;
        }

        moveInputVector = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        moveInputVector = Vector2.ClampMagnitude(moveInputVector, 1f);

        dashRequested |= Input.GetKeyDown(KeyCode.LeftShift);

        if (Input.GetKeyDown(KeyCode.K) && player != null)
            player.OnSkillInput();
    }

    public void Initialize(PlayerData data)
    {
        playerData = data ?? new PlayerData();
    }

    private void FixedUpdate()
    {
        moveInputVector = Vector2.ClampMagnitude(moveInputVector, 1f);

        if (IsStunned)
        {
            stunTimer = Mathf.Max(0f, stunTimer - Time.fixedDeltaTime);
            body.linearVelocity = Vector2.MoveTowards(body.linearVelocity, Vector2.zero, 20f * Time.fixedDeltaTime);
            UpdateAnimationParams();
            return;
        }

        if (dashCooldownTimer > 0f)
            dashCooldownTimer = Mathf.Max(0f, dashCooldownTimer - Time.fixedDeltaTime);

        // 朝向
        if (dashTime <= 0f && moveInputVector.sqrMagnitude > 0.001f)
        {
            lastMoveDirection = moveInputVector.normalized;
            if (Mathf.Abs(moveInputVector.x) > 0.01f)
            {
                facingDirection = Mathf.Sign(moveInputVector.x);
                ApplyFacing();
            }
        }

        // 冲刺
        if (dashRequested && dashTime <= 0f && dashCooldownTimer <= 0f)
        {
            dashDirection = lastMoveDirection;
            dashTime = Mathf.Max(dashDuration * (player != null ? player.DashDistanceMultiplier : 1f), Time.fixedDeltaTime);

            float cooldownMult = player != null ? player.DashCooldownMultiplier : 1f;
            dashCooldownTimer = playerData.DashCooldown * cooldownMult;

            AudioManager.Instance?.PlaySound(AudioManager.DashSoundPath);

            if (animator != null)
                animator.SetTrigger("IsDash");

            if (player != null)
                player.OnDashPerformed();
        }
        dashRequested = false;

        // 移动
        float moveSpeed = playerData.MoveSpeed * (1f + (player != null ? player.MoveSpeedBonus : 0f));

        body.linearVelocity = dashTime > 0f
            ? dashDirection * dashSpeed
            : moveInputVector * moveSpeed;

        UpdateAnimationParams();
        dashTime = Mathf.Max(0f, dashTime - Time.fixedDeltaTime);
    }

    /// <summary>外部施加眩晕（比如敌人控制技能）。</summary>
    public void ApplyStun(float duration)
    {
        if (duration <= 0f) return;
        stunTimer = Mathf.Max(stunTimer, duration);
        dashTime = 0f;
        dashRequested = false;
        moveInputVector = Vector2.zero;
        body.linearVelocity = Vector2.zero;
    }

    private void OnDisable()
    {
        moveInputVector = Vector2.zero;
        lastMoveDirection = new Vector2(facingDirection, 0f);
        dashTime = 0f;
        dashCooldownTimer = 0f;
        stunTimer = 0f;
        dashRequested = false;
        if (body != null)
            body.linearVelocity = Vector2.zero;
    }

    private void UpdateAnimationParams()
    {
        if (animator == null)
            return;
        animator.SetBool("IsRun", moveInputVector.sqrMagnitude > 0.001f);
    }

    private void ApplyFacing()
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * facingDirection;
        transform.localScale = scale;
    }
}
