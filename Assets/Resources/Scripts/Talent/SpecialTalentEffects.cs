using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>不可升级特殊天赋的角色专属效果与屏幕特效。</summary>
public sealed class SpecialTalentEffects
{
    private readonly Player owner;
    private readonly bool[] acquired = new bool[System.Enum.GetValues(typeof(SpecialEffect)).Length];
    private float convertedMaxHealth;
    private float pentagramCooldown;
    private float pentagramVisibleTime;
    private GameObject pentagramCanvas;
    private Image pentagramImage;

    public SpecialTalentEffects(Player owner) => this.owner = owner;

    public bool HasHealthLock => Has(SpecialEffect.CursedBlade) || Has(SpecialEffect.HealthToShield);
    public bool PreventShieldDecay => Has(SpecialEffect.HealthToShield);
    public float ConvertedMaxHealth => convertedMaxHealth;
    public float WeaponCooldownSlowMultiplier => Has(SpecialEffect.EchoForm) ? 2f : 1f;
    public int ProjectileCountMultiplier => Has(SpecialEffect.EchoForm) ? 2 : 1;

    public bool Has(SpecialEffect effect) => (int)effect >= 0 && (int)effect < acquired.Length && acquired[(int)effect];

    public bool CanAcquire(SpecialTalent talent)
    {
        if (talent == null) return false;
        bool locksHealth = talent.effect == SpecialEffect.CursedBlade || talent.effect == SpecialEffect.HealthToShield;
        if (locksHealth && Has(SpecialEffect.ShieldToHealth)) return false;
        if (talent.effect == SpecialEffect.ShieldToHealth && HasHealthLock) return false;
        return !Has(talent.effect);
    }

    public void Configure(SpecialTalent talent)
    {
        if (talent == null || Has(talent.effect)) return;
        if (!CanAcquire(talent)) return;

        float previousMaxHealth = owner.UnlockedMaxHealth;
        acquired[(int)talent.effect] = true;
        if (talent.effect == SpecialEffect.CursedBlade || talent.effect == SpecialEffect.HealthToShield)
        {
            owner.ClampHealthToMaximum();
            if (talent.effect == SpecialEffect.HealthToShield)
                owner.AddShield(Mathf.Max(0f, previousMaxHealth - 1f) * 2f);
        }
        else if (talent.effect == SpecialEffect.Pentagram)
        {
            pentagramCooldown = 90f;
        }
        else if (talent.effect == SpecialEffect.EchoForm)
        {
            owner.RefreshOrbitWeapons();
        }
        else if (talent.effect == SpecialEffect.Brimstone)
        {
            owner.AddOrUpgradeWeapon(new BrimstoneWeapon
            {
                id = talent.id,
                displayName = talent.displayName,
                cooldown = Mathf.Max(0.05f, talent.brimstoneCooldown),
                damage = Mathf.Max(0, talent.brimstoneDamage),
                range = Mathf.Max(0.1f, talent.brimstoneRange),
                beamWidth = Mathf.Max(0.05f, talent.brimstoneWidth),
                beamDuration = Mathf.Max(0.01f, talent.brimstoneDuration),
                stackUpMode = StackUpMode.ExtraShot
            }, 1);
        }
    }

    public void OnMaxHealthIncreased(float amount)
    {
        if (Has(SpecialEffect.HealthToShield) && amount > 0f)
            owner.AddShield(amount * 2f);
    }

    public bool ConvertShieldToHealth(float amount)
    {
        if (!Has(SpecialEffect.ShieldToHealth) || amount <= 0f) return false;
        convertedMaxHealth += amount * 0.5f;
        return true;
    }

    public float ApplyDamageMultiplier(float multiplier)
    {
        if (Has(SpecialEffect.VitalityPower))
            multiplier *= 1f + Mathf.Max(0f, owner.CurrentHealth) * 0.01f;
        if (Has(SpecialEffect.ShieldPower))
            multiplier *= 1f + Mathf.Max(0f, owner.CurrentShield) * 0.01f;
        if (Has(SpecialEffect.CursedBlade))
            multiplier *= 3f;
        return multiplier;
    }

    public int GetProjectileCount(int count) => Mathf.Max(0, count) * ProjectileCountMultiplier;

    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f) return;

        if (pentagramVisibleTime > 0f)
        {
            pentagramVisibleTime = Mathf.Max(0f, pentagramVisibleTime - deltaTime);
            if (pentagramImage != null)
            {
                Color color = pentagramImage.color;
                color.a = Mathf.Clamp01(pentagramVisibleTime / 0.25f);
                pentagramImage.color = color;
            }
            if (pentagramVisibleTime <= 0f) ClearPentagramVisual();
        }

        if (owner == null || !owner.isActiveAndEnabled || owner.IsDead ||
            (GameManager.Instance != null && GameManager.Instance.IsGameOver) || !Has(SpecialEffect.Pentagram)) return;
        pentagramCooldown = Mathf.Max(0f, pentagramCooldown - deltaTime);
        if (pentagramCooldown <= 0f) TryActivatePentagram();
    }

    public bool TryActivatePentagram()
    {
        if (!Has(SpecialEffect.Pentagram) || owner == null || !owner.isActiveAndEnabled || owner.IsDead ||
            (GameManager.Instance != null && GameManager.Instance.IsGameOver) || pentagramCooldown > 0f)
        {
            return false;
        }

        pentagramCooldown = 90f;
        pentagramVisibleTime = 1f;
        ShowPentagram();

        Camera camera = Camera.main;
        if (camera == null || camera.gameObject.scene != owner.gameObject.scene)
        {
            foreach (Camera candidate in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (candidate != null && candidate.isActiveAndEnabled &&
                    candidate.gameObject.scene == owner.gameObject.scene)
                {
                    camera = candidate;
                    break;
                }
            }
        }
        if (camera == null) return true;

        foreach (Enemy enemy in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (!owner.CanTargetEnemy(enemy)) continue;
            Vector3 viewport = camera.WorldToViewportPoint(enemy.transform.position);
            if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f)
                continue;
            enemy.EliminateWithoutExperience();
        }
        return true;
    }

    public void ClearVisual()
    {
        pentagramVisibleTime = 0f;
        ClearPentagramVisual();
    }

    private void ShowPentagram()
    {
        ClearPentagramVisual();

        pentagramCanvas = new GameObject("Pentagram Screen Effect", typeof(RectTransform), typeof(Canvas));
        SceneManager.MoveGameObjectToScene(pentagramCanvas, owner.gameObject.scene);
        Canvas canvas = pentagramCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10000;

        GameObject imageObject = new GameObject("Pentagram Image", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(pentagramCanvas.transform, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        pentagramImage = imageObject.GetComponent<Image>();
        pentagramImage.sprite = CreatePentagramSprite();
        pentagramImage.raycastTarget = false;
        pentagramImage.color = new Color(1f, 1f, 1f, 1f);
    }

    private static Sprite CreatePentagramSprite()
    {
        const int size = 512;
        const float half = size * 0.5f;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Pentagram Screen Texture",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Vector2[] vertices = new Vector2[5];
        for (int i = 0; i < vertices.Length; i++)
        {
            float angle = (90f - i * 72f) * Mathf.Deg2Rad;
            vertices[i] = new Vector2(half + Mathf.Cos(angle) * 220f, half + Mathf.Sin(angle) * 220f);
        }
        int[] order = { 0, 2, 4, 1, 3, 0 };
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 point = new Vector2(x, y);
                float distance = float.MaxValue;
                for (int i = 0; i < order.Length - 1; i++)
                    distance = Mathf.Min(distance, DistanceToSegment(point, vertices[order[i]], vertices[order[i + 1]]));

                float halo = Mathf.Clamp01((18f - distance) / 18f) * 0.22f;
                float line = Mathf.Clamp01((8f - distance) / 2f);
                Color glow = new Color(0.9f, 0.04f, 0.12f, halo);
                Color core = new Color(1f, 0.78f, 0.28f, line);
                pixels[y * size + x] = Color.Lerp(glow, core, line);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 delta = end - start;
        float t = Mathf.Clamp01(Vector2.Dot(point - start, delta) / delta.sqrMagnitude);
        return Vector2.Distance(point, start + delta * t);
    }

    private void ClearPentagramVisual()
    {
        if (pentagramImage != null && pentagramImage.sprite != null)
        {
            Texture texture = pentagramImage.sprite.texture;
            Object.Destroy(pentagramImage.sprite);
            Object.Destroy(texture);
        }
        if (pentagramCanvas != null) Object.Destroy(pentagramCanvas);
        pentagramImage = null;
        pentagramCanvas = null;
    }
}
