using UnityEngine;

/// <summary>Moves toward the player while protecting itself with a forward-facing regenerating shield.</summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ShieldEnemy : Enemy
{
    [Header("护盾")]
    [SerializeField, Min(1f)] private float shieldMaxHealth = 60f;
    [SerializeField, Min(0f)] private float shieldDetectionRadius = 12f;
    [SerializeField, Range(0f, 180f)] private float shieldFrontAngle = 140f;
    [SerializeField, Min(0f)] private float shieldForwardOffset = 0.8f;
    [SerializeField, Min(0f)] private float shieldRegenDelay = 3f;
    [SerializeField, Min(0f)] private float shieldRegenRate = 12f;

    [Header("移动")]
    [SerializeField, Min(0f)] private float guardDistance = 3f;

    private static Sprite sharedShieldSprite;
    private static int spriteUserCount;

    private Rigidbody2D body;
    private Vector2 desiredVelocity;
    private Transform shieldVisual;
    private SpriteRenderer shieldRenderer;
    private float currentShieldHealth;
    private float shieldRegenTimer;
    private Vector2 shieldDirection = Vector2.right;
    private bool registeredSpriteUser;

    public float ShieldMaxHealth => Mathf.Max(1f, shieldMaxHealth);
    public float CurrentShieldHealth => currentShieldHealth;
    public Vector2 ShieldDirection => shieldDirection;

    protected override void Awake()
    {
        base.Awake();
        body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.gravityScale = 0f;
            body.constraints |= RigidbodyConstraints2D.FreezeRotation;
        }

        CreateShieldVisual();
        spriteUserCount++;
        registeredSpriteUser = true;
        UpdateShieldFacing();
    }

    protected override void Update()
    {
        base.Update();
        if (IsDead) return;

        UpdateShieldFacing();
        UpdateShieldRecharge(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        if (body == null) return;
        body.linearVelocity = IsDead ? Vector2.zero : desiredVelocity;
    }

    protected override void MoveTowardsPlayer()
    {
        if (body == null) body = GetComponent<Rigidbody2D>();
        Player target = Player.ActivePlayer;
        if (IsStunned || target == null || target.IsDead || data == null || data.MoveSpeed <= 0f)
        {
            desiredVelocity = Vector2.zero;
            return;
        }

        Vector2 toPlayer = (Vector2)target.transform.position - (Vector2)transform.position;
        if (toPlayer.sqrMagnitude <= guardDistance * guardDistance)
        {
            desiredVelocity = Vector2.zero;
            return;
        }

        desiredVelocity = toPlayer.normalized * data.MoveSpeed * MoveSpeedMultiplier;
    }

    protected override float AbsorbDamage(float damage, Vector2? sourcePosition)
    {
        if (damage <= 0f || currentShieldHealth <= 0f || !TryGetShieldTarget(out Player target))
            return damage;

        Vector2 toPlayer = (Vector2)target.transform.position - (Vector2)transform.position;
        Vector2 incomingFrom = sourcePosition ?? target.transform.position;
        Vector2 toSource = incomingFrom - (Vector2)transform.position;
        if (toPlayer.sqrMagnitude > 0.0001f && toSource.sqrMagnitude > 0.0001f &&
            Vector2.Angle(toPlayer, toSource) > shieldFrontAngle * 0.5f)
            return damage;

        float absorbed = Mathf.Min(currentShieldHealth, damage);
        currentShieldHealth -= absorbed;
        shieldRegenTimer = shieldRegenDelay;
        UpdateShieldVisualState();
        return damage - absorbed;
    }

    protected override void ResetState()
    {
        base.ResetState();
        currentShieldHealth = ShieldMaxHealth;
        shieldRegenTimer = 0f;
        UpdateShieldVisualState();
    }

    private void UpdateShieldFacing()
    {
        bool hasTarget = TryGetShieldTarget(out Player target);
        if (hasTarget)
        {
            Vector2 toPlayer = (Vector2)target.transform.position - (Vector2)transform.position;
            if (toPlayer.sqrMagnitude > 0.0001f)
                shieldDirection = toPlayer.normalized;
        }

        if (shieldVisual != null)
        {
            shieldVisual.position = transform.position + (Vector3)(shieldDirection * shieldForwardOffset);
            float angle = Mathf.Atan2(shieldDirection.y, shieldDirection.x) * Mathf.Rad2Deg;
            shieldVisual.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }
        UpdateShieldVisualState(hasTarget);
    }

    private bool TryGetShieldTarget(out Player target)
    {
        target = Player.ActivePlayer;
        if (target == null || !target.isActiveAndEnabled || target.IsDead)
        {
            target = null;
            return false;
        }

        float radius = Mathf.Max(0f, shieldDetectionRadius);
        return ((Vector2)target.transform.position - (Vector2)transform.position).sqrMagnitude <= radius * radius;
    }

private void UpdateShieldRecharge(float deltaTime)
    {
        if (currentShieldHealth >= ShieldMaxHealth || shieldRegenRate <= 0f || deltaTime <= 0f) return;
        float rechargeTime = deltaTime;
        if (shieldRegenTimer > 0f)
        {
            float delayElapsed = Mathf.Min(shieldRegenTimer, rechargeTime);
            shieldRegenTimer -= delayElapsed;
            rechargeTime -= delayElapsed;
            if (rechargeTime <= 0f) return;
        }

        currentShieldHealth = Mathf.Min(ShieldMaxHealth, currentShieldHealth + shieldRegenRate * rechargeTime);
        UpdateShieldVisualState();
    }

    private void CreateShieldVisual()
    {
        if (shieldVisual != null) return;

        var visualObject = new GameObject("Front Shield");
        shieldVisual = visualObject.transform;
        shieldVisual.SetParent(transform, false);
        shieldRenderer = visualObject.AddComponent<SpriteRenderer>();
        shieldRenderer.sprite = GetShieldSprite();
        shieldRenderer.color = new Color(0.38f, 0.88f, 1f, 0.9f);
        shieldRenderer.sortingOrder = 10;
        shieldVisual.localScale = new Vector3(1.4f, 1f, 1f);
    }

    private void UpdateShieldVisualState(bool? hasTarget = null)
    {
        if (shieldRenderer != null)
            shieldRenderer.enabled = (hasTarget ?? TryGetShieldTarget(out _)) && currentShieldHealth > 0f;
    }

    private static Sprite GetShieldSprite()
    {
        if (sharedShieldSprite != null) return sharedShieldSprite;

        const int size = 96;
        Vector2[] shape =
        {
            new Vector2(0.12f, 0.15f),
            new Vector2(0.88f, 0.15f),
            new Vector2(0.92f, 0.55f),
            new Vector2(0.68f, 0.84f),
            new Vector2(0.5f, 1f),
            new Vector2(0.32f, 0.84f),
            new Vector2(0.08f, 0.55f)
        };

        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Shield Enemy Sprite",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 point = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                bool inside = IsInside(point, shape);
                float edgeDistance = DistanceToEdge(point, shape) * size;
                float edge = 1f - Mathf.Clamp01(edgeDistance / 5f);
                float alpha = inside ? 0.2f + 0.55f * edge : 0.16f * (1f - Mathf.Clamp01(edgeDistance / 7f));
                pixels[y * size + x] = new Color(0.3f + 0.55f * edge, 0.82f + 0.18f * edge, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        sharedShieldSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 64f);
        return sharedShieldSprite;
    }

    private static bool IsInside(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[j];
            bool crosses = (a.y > point.y) != (b.y > point.y) &&
                point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x;
            if (crosses) inside = !inside;
        }
        return inside;
    }

    private static float DistanceToEdge(Vector2 point, Vector2[] polygon)
    {
        float closest = float.MaxValue;
        for (int i = 0; i < polygon.Length; i++)
        {
            Vector2 start = polygon[i];
            Vector2 edge = polygon[(i + 1) % polygon.Length] - start;
            float t = Mathf.Clamp01(Vector2.Dot(point - start, edge) / edge.sqrMagnitude);
            closest = Mathf.Min(closest, Vector2.Distance(point, start + edge * t));
        }
        return closest;
    }

    private void OnDestroy()
    {
        if (!registeredSpriteUser) return;
        registeredSpriteUser = false;
        spriteUserCount = Mathf.Max(0, spriteUserCount - 1);
        if (spriteUserCount == 0 && sharedShieldSprite != null)
        {
            Texture2D texture = sharedShieldSprite.texture;
            Destroy(sharedShieldSprite);
            Destroy(texture);
            sharedShieldSprite = null;
        }
    }
}
