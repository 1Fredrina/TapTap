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
    private float moveInput;
    private float facingDirection = 1f;
    private float dashTime;
    private bool jumpRequested;
    private bool dashRequested;
    public float FacingDirection => facingDirection;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        player = GetComponent<Player>();
        body.constraints |= RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        groundFilter.SetLayerMask(groundLayers);
        groundFilter.SetNormalAngle(45f, 135f);
        groundFilter.useTriggers = false;
    }

    private void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");
        jumpRequested |= Input.GetKeyDown(KeyCode.Space);
        dashRequested |= Input.GetKeyDown(KeyCode.LeftShift);

        if (Input.GetKeyDown(KeyCode.J) && player != null)
        {
            player.OnAttackInput();
        }
    }

    public void Initialize(PlayerData data)
    {
        playerData = data;
    }

    private void FixedUpdate()
    {
        bool grounded = body.IsTouching(groundFilter);

        if (dashTime <= 0f && moveInput != 0f)
            facingDirection = Mathf.Sign(moveInput);
            ApplyFacing();

        if (dashRequested && dashTime <= 0f)
        {
            dashTime = Mathf.Max(dashDuration, Time.fixedDeltaTime);
            if (animator != null)
                animator.SetTrigger("IsDash");
        }

        dashRequested = false;
        Vector2 velocity = body.linearVelocity;
        velocity.x = dashTime > 0f
            ? facingDirection * dashSpeed
            : moveInput * playerData.MoveSpeed;

        if (jumpRequested && velocity.y <= 0.05f && grounded)
        {
            float gravity = -Physics2D.gravity.y * body.gravityScale;
            if (gravity > 0f && playerData.JumpHeight > 0f)
            {
                velocity.y = Mathf.Sqrt(2f * gravity * playerData.JumpHeight);
                if (animator != null)
                    animator.SetTrigger("IsJump");
            }
        }

        jumpRequested = false;
        body.linearVelocity = velocity;
        UpdateAnimationParams();
        dashTime = Mathf.Max(0f, dashTime - Time.fixedDeltaTime);
    }

    private void OnDisable()
    {
        moveInput = 0f;
        dashTime = 0f;
        jumpRequested = false;
        dashRequested = false;
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