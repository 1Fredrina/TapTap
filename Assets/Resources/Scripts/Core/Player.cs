using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private PlayerData data;
    [SerializeField] private PlayerController controller;
    [SerializeField] private LayerMask enemyLayers = ~0;

    private static Player activePlayer;
    private int level = 1;
    private int experience;
    private int damageLevelBonus;
    private float attackSpeedMultiplier = 1f;
    private float attackRangeBonus;

    // ===== 武器 =====
    private readonly List<WeaponBase> weapons = new();
    public IReadOnlyList<WeaponBase> Weapons => weapons;

    // ===== 天赋 =====
    private readonly List<TalentRuntime> acquiredTalents = new();
    private readonly Dictionary<TalentColor, int> colorCounts = new();
    private readonly Dictionary<StatType, float> statBonuses = new();

    public IReadOnlyList<TalentRuntime> AcquiredTalents => acquiredTalents;

    public PlayerData Data => data;
    public PlayerController Controller => controller;
    public bool IsInvincible => invincibleTimer > 0f;
    public SkillBase Skill => skill;
    public static Player ActivePlayer => activePlayer;
    public int Level => level;
    public int Experience => experience;
    public int ExperienceToNextLevel => Mathf.Max(1, (data != null ? data.BaseExperienceToNextLevel : 5) * level);

    public int AttackDamage => (data != null ? data.AttackDamage : 1) + damageLevelBonus;
    public float AttackInterval => Mathf.Max(0.08f, (data != null ? data.AttackInterval : 0.5f) * attackSpeedMultiplier);
    public float AttackRange => Mathf.Max(0f, (data != null ? data.AttackRange : 12f) + attackRangeBonus);

    private SkillBase skill;
    private DamageBuffSkill damageBuff;
    private DoubleActionSkill doubleAction;

    private SpriteRenderer[] allRenderers;
    private float invincibleTimer;
    private float blinkTimer;
    private bool[] initialEnabledStates;

    public bool DoubleActionActive => GameManager.Instance != null
        ? GameManager.Instance.TeamDoubleActionActive
        : doubleAction != null && doubleAction.IsActive;

    public void Initialize(PlayerData d)
    {
        data = d ?? new PlayerData();
        if (controller == null)
            controller = GetComponent<PlayerController>();
        if (controller != null)
            controller.Initialize(data);

        level = 1;
        experience = 0;
        damageLevelBonus = 0;
        attackSpeedMultiplier = 1f;
        attackRangeBonus = 0f;
        skill = null;
        damageBuff = null;
        doubleAction = null;

        weapons.Clear();
        acquiredTalents.Clear();
        colorCounts.Clear();
        statBonuses.Clear();

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
        Initialize(data);
        allRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        initialEnabledStates = new bool[allRenderers.Length];
        for (int i = 0; i < allRenderers.Length; i++)
            initialEnabledStates[i] = allRenderers[i].enabled;
    }

    private void OnEnable()
    {
        if (activePlayer == null)
            activePlayer = this;
    }

    private void OnDisable()
    {
        if (activePlayer == this)
            activePlayer = null;
    }

    private void Update()
    {
        UpdateInvincible();
        if (damageBuff != null)
            damageBuff.Tick(Time.deltaTime);
        if (doubleAction != null) doubleAction.Tick(Time.deltaTime);

        TickWeapons();
    }

    private void TickWeapons()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;
        if (controller != null && (!controller.enabled || controller.IsStunned))
            return;

        for (int i = 0; i < weapons.Count; i++)
        {
            WeaponBase weapon = weapons[i];
            if (weapon == null) continue;
            if (!weapon.TickCooldown(Time.deltaTime)) continue;

            Enemy target = FindClosestEnemyInRange(weapon.range);
            if (target == null) continue;

            weapon.Fire(this, target);
            //weapon.ResetCooldown(WeaponCooldownMultiplier);
        }
    }

    public void OnSkillInput()
    {
        GameManager manager = GameManager.Instance;
        if (manager == null || manager.IsGameOver) return;
        if (controller != null && controller.IsStunned) return;
        if (skill == null) return;
        if (!manager.TryStartSkillCooldown(this, skill.Cooldown)) return;

        skill.Cast(this);
    }

    private Enemy FindClosestEnemyInRange(float range)
    {
        Enemy closest = null;
        float closestDistance = range * range;
        Enemy[] enemies = FindObjectsByType<Enemy>();

        foreach (Enemy enemy in enemies)
        {
            if (enemy == null || enemy.IsDead || !enemy.isActiveAndEnabled) continue;
            if (enemy.gameObject.scene != gameObject.scene) continue;
            if ((enemyLayers.value & (1 << enemy.gameObject.layer)) == 0) continue;

            float distance = ((Vector2)enemy.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (distance <= closestDistance)
            {
                closestDistance = distance;
                closest = enemy;
            }
        }

        return closest;
    }

    public void AddExperience(int amount)
    {
        if (amount <= 0 || data == null) return;

        float expMult = Mathf.Max(0f, data.ExperienceMultiplier) * (1f + GetStatBonus(StatType.ExperienceGain));
        experience += Mathf.RoundToInt(amount * expMult);

        while (experience >= ExperienceToNextLevel)
        {
            if (level >= data.MaxLevel)
            {
                experience = 0;
                break;
            }

            experience -= ExperienceToNextLevel;
            level++;
            damageLevelBonus++;
            attackSpeedMultiplier *= 0.95f;
            attackRangeBonus += 0.25f;
            SendMessage("OnLevelUp", level, SendMessageOptions.DontRequireReceiver);
        }
    }

    public static void NotifyEnemyDefeated(int reward)
    {
        if (activePlayer != null)
            activePlayer.AddExperience(reward);
    }

    public void TakeDamage(int dmg = 1)
    {
        TakeDamage(dmg, (Vector2)transform.position);
    }

    public void TakeDamage(int dmg, Vector2 sourcePosition)
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.IsGameOver) return;
        if (IsInvincible || dmg <= 0) return;

        GameManager.Instance.LoseLife(dmg);
        StartInvincible();
    }

    private void StartInvincible()
    {
        float bonus = GetStatBonus(StatType.InvincibleTime);
        invincibleTimer = (data != null ? data.InvincibleTime : 1f) * (1f + bonus);
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
        if (allRenderers == null || allRenderers.Length == 0) return;
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
            float mult = 1f;
            if (damageBuff != null && damageBuff.IsActive)
                mult *= damageBuff.Multiplier;
            mult *= (1f + GetStatBonus(StatType.AttackDamage));
            return mult;
        }
    }

    public float WeaponCooldownMultiplier
    {
        get
        {
            if (GameManager.Instance != null && GameManager.Instance.TeamDoubleActionActive)
                return GameManager.Instance.TeamWeaponCooldownMultiplier;
            if (doubleAction != null && doubleAction.IsActive)
                return doubleAction.WeaponCooldownMultiplier;
            return 1f;
        }
    }

    public float MoveSpeedBonus
    {
        get
        {
            float bonus = GetStatBonus(StatType.MoveSpeed);
            if (GameManager.Instance != null && GameManager.Instance.TeamDoubleActionActive)
                bonus += GameManager.Instance.TeamMoveSpeedBonus;
            else if (doubleAction != null && doubleAction.IsActive)
                bonus += doubleAction.MoveSpeedBonus;
            return bonus;
        }
    }

    // ===== 武器 =====

    public void AddOrUpgradeWeapon(WeaponBase weapon, int stack)
    {
        if (weapon == null) return;

        WeaponBase existing = weapons.Find(w => w.id == weapon.id);
        if (existing == null)
        {
            weapon.stack = stack;
            weapons.Add(weapon);
        }
        else
        {
            existing.OnStackUp(stack);
        }
    }

    // ===== 天赋 =====

    public int GetColorCount(TalentColor color)
    {
        return colorCounts.TryGetValue(color, out int count) ? count : 0;
    }

    public bool CanAcquireColor(TalentColor color)
    {
        return GetColorCount(color) < TalentBase.MaxPerColor;
    }

    public int GetTalentStack(TalentBase talent)
    {
        var runtime = acquiredTalents.Find(t => t.data == talent);
        return runtime?.stack ?? 0;
    }

    public bool TryAddTalent(TalentBase talent)
    {
        if (talent == null) return false;
        if (!CanAcquireColor(talent.color)) return false;

        var runtime = acquiredTalents.Find(t => t.data == talent);
        if (runtime == null)
        {
            runtime = new TalentRuntime { data = talent, stack = 0 };
            acquiredTalents.Add(runtime);
        }

        if (runtime.stack >= talent.maxStack) return false;

        runtime.stack++;
        colorCounts[talent.color] = GetColorCount(talent.color) + 1;
        talent.OnAcquire(this, runtime.stack);
        return true;
    }


    public float GetStatBonus(StatType type)
    {
        return statBonuses.TryGetValue(type, out float v) ? v : 0f;
    }

    public void ApplyStatTalent(StatType type, float value, int stack)
    {
        statBonuses[type] = GetStatBonus(type) + value;
    }
}