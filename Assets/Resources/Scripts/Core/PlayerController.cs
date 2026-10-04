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
    private ContactFilter2D groundFilter;
    private float moveInput;
    private float facingDirection = 1f;
    private float dashTime;
    private bool jumpRequested;
    private bool dashRequested;
    private bool jumpAnimationRequested;
    private string currentAnimation;

    private const string MoveState = "CellCharacter_Move";
    private const string JumpState = "CellCharacter_Jump";
    private const string DashState = "CellCharacter_Dash";

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        body.constraints |= RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        groundFilter.SetLayerMask(groundLayers);
        groundFilter.SetNormalAngle(45f, 135f); // 只把脚下的支撑面视为地面。
        groundFilter.useTriggers = false;
    }

    private void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");
        jumpRequested |= Input.GetKeyDown(KeyCode.Space);
        dashRequested |= Input.GetKeyDown(KeyCode.LeftShift);
    }

    public void Initialize(PlayerData data)
    {
        playerData = data;
    }
    private void FixedUpdate()
    {
        Debug.Log($"active={gameObject.name}, velocity={body.linearVelocity}, input={moveInput}");
        bool grounded = body.IsTouching(groundFilter);
        if (dashTime <= 0f && moveInput != 0f)
            facingDirection = Mathf.Sign(moveInput);

        if (dashRequested && dashTime <= 0f)
            dashTime = Mathf.Max(dashDuration, Time.fixedDeltaTime);

        dashRequested = false;
        Vector2 velocity = body.linearVelocity;
        velocity.x = dashTime > 0f
            ? facingDirection * dashSpeed
            : moveInput * playerData.MoveSpeed;

        bool jumpedThisFrame = false;
        if (jumpRequested && velocity.y <= 0.05f && grounded)
        {
            float gravity = -Physics2D.gravity.y * body.gravityScale;
            if (gravity > 0f && playerData.JumpHeight > 0f)
            {
                velocity.y = Mathf.Sqrt(2f * gravity * playerData.JumpHeight);
                jumpAnimationRequested = true;
                jumpedThisFrame = true;
            }
        }

        jumpRequested = false;
        body.linearVelocity = velocity;
        UpdateAnimation(grounded || jumpedThisFrame);
        dashTime = Mathf.Max(0f, dashTime - Time.fixedDeltaTime);
    }

    private void OnDisable()
    {
        moveInput = 0f;
        dashTime = 0f;
        jumpRequested = false;
        jumpAnimationRequested = false;
        dashRequested = false;
        if (body != null)
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
    }

    private void UpdateAnimation(bool grounded)
    {
        if (animator == null)
            return;

        bool dashing = dashTime > 0f;
        bool airborne = !grounded || body.linearVelocity.y > 0.05f;
        animator.SetBool("IsRun", !dashing && grounded && Mathf.Abs(moveInput) > 0.01f);
        animator.SetBool("IsJump", !dashing && airborne);
        animator.SetBool("IsDash", dashing);

        // Dash has priority over all other locomotion states for its full duration.
        string state = dashing ? DashState : (airborne || jumpAnimationRequested ? JumpState : MoveState);
        if (state != currentAnimation)
        {
            animator.CrossFadeInFixedTime(state, 0.05f);
            currentAnimation = state;
        }
    }
}
