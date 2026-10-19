using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private PlayerData data;
    [SerializeField] private PlayerController controller;
    [SerializeField] private LayerMask enemyLayers = ~0;

    [Header("升级抽天赋")]
    [SerializeField, Min(1)] private int baseChoicesPerLevel = 3;

    private float moveSpeedToDamageRatio = 0f;

    private static Player activePlayer;
    private int level = 1;
    private int experience;

    // ===== 生命 =====
    private float currentHealth;
    private float currentShield;

    // ===== 伤害成长 =====
    private float baseDamageMultiplier = 1f;
    private float killStackBonusPerKill = 0f;

    // ===== 静止蓄力 =====
    private float stationaryTime = 0f;
    private const float stationaryMaxTime = 3f;
    private float stationaryAttackSpeedMax = 0f;
    private float stationarySizeMax = 0f;
    private float baseSizeBonus = 0f;
    private float stationarySizeBonus = 0f;

    // ===== 冲刺 Buff =====
    private float dashBuffTimer = 0f;
    private float dashBuffDuration = 0f;
    private float dashBuffDamageBonus = 0f;

    // ===== 触发型天赋（配置与状态均属于当前角色） =====
    private float dashPulseRadius, dashPulseStunDuration, dashPulseCooldown, dashPulseTimer;
    private int dashPulseDamage;
    private GameObject dashPulseEffectPrefab;
    private float frenzyDuration, frenzyTimer, frenzyAttackSpeedPerCharge, frenzyMoveSpeedPerCharge;
    private int frenzyMaxCharges, frenzyCharges;
    private float overhealShieldRatio, overhealShieldCapPercent;
    private int retaliationCount, retaliationDamage;
    private float retaliationCooldown, retaliationTimer;
    private GameObject retaliationPrefab;

    public int KillFrenzyCharges => frenzyCharges;
    public float KillFrenzyTimeRemaining => frenzyTimer;
    public PlayerTalentEffects TalentEffects { get; private set; }
    public DerivedTalentEffects DerivedEffects { get; private set; }
    public AdvancedTalentEffects AdvancedEffects { get; private set; }
    public SpecialTalentEffects SpecialEffects { get; private set; }
    internal LayerMask EnemyLayers => enemyLayers;

    // ===== 武器 =====
    private readonly List<WeaponBase> weapons = new();
    public IReadOnlyList<WeaponBase> Weapons => weapons;

    // ===== 天赋 =====
    private readonly List<TalentRuntime> acquiredTalents = new();
    private readonly Dictionary<TalentColor, int> colorCounts = new();
    private readonly Dictionary<StatType, float> statBonuses = new();
    private readonly List<TalentBase> pendingChoices = new();

    public IReadOnlyList<TalentRuntime> AcquiredTalents => acquiredTalents;
    public IReadOnlyList<TalentBase> PendingChoices => pendingChoices;

    public PlayerData Data => data;
    public PlayerController Controller => controller;
    public bool IsInvincible => invincibleTimer > 0f;
    public bool IsDead => currentHealth <= 0f;
    public SkillBase Skill => skill;
    public static Player ActivePlayer => activePlayer;
    public int Level => level;
    public int Experience => experience;
    public int ExperienceToNextLevel => Mathf.Max(1, (data != null ? data.BaseExperienceToNextLevel : 5) * level);

    public int AttackDamage => data != null ? data.AttackDamage : 1;
    public float AttackInterval => data != null ? data.AttackInterval : 0.5f;
    public float AttackRange => data != null ? data.AttackRange : 12f;

    public float MaxHealth
    {
        get
        {
            return SpecialEffects != null && SpecialEffects.HasHealthLock ? 1f : UnlockedMaxHealth;
        }
    }

    public float UnlockedMaxHealth
    {
        get
        {
            float baseHp = data != null ? data.MaxHealth : 10f;
            return baseHp * (1f + GetStatBonus(StatType.MaxHealth)) +
                (SpecialEffects != null ? SpecialEffects.ConvertedMaxHealth : 0f);
        }
    }

    public float CurrentHealth => currentHealth;
    public float CurrentShield => currentShield;

    public int ExtraPierce => Mathf.RoundToInt(GetStatBonus(StatType.ExtraPierce));

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
        baseDamageMultiplier = 1f;
        killStackBonusPerKill = 0f;
        stationaryTime = 0f;
        stationaryAttackSpeedMax = 0f;
        stationarySizeMax = 0f;
        baseSizeBonus = 0f;
        stationarySizeBonus = 0f;
        dashBuffTimer = 0f;
        dashBuffDuration = 0f;
        dashBuffDamageBonus = 0f;
        moveSpeedToDamageRatio = 0f;
        ResetReactiveTalents();
        TalentEffects = new PlayerTalentEffects(this);
        DerivedEffects = new DerivedTalentEffects(this);
        AdvancedEffects = new AdvancedTalentEffects(this);
        SpecialEffects?.ClearVisual();
        SpecialEffects = new SpecialTalentEffects(this);
        skill = null;
        damageBuff = null;
        doubleAction = null;

        foreach (var w in weapons)
        {
            if (w is OrbitWeapon orbit) orbit.Clear();
            if (w is AuraWeapon aura) aura.Clear();
        }

        weapons.Clear();
        acquiredTalents.Clear();
        colorCounts.Clear();
        statBonuses.Clear();
        pendingChoices.Clear();

        currentHealth = MaxHealth;
        currentShield = 0f;

        ApplySizeCombined();

        if (data != null && !string.IsNullOrEmpty(data.skillId))
        {
            skill = SkillFactory.Create(data.skillId);
            if (skill != null && data.skillCooldown > 0f)
                skill.Cooldown = data.skillCooldown;
            damageBuff = skill as DamageBuffSkill;
            doubleAction = skill as DoubleActionSkill;
        }

        // 初始武器
        if (data != null && !string.IsNullOrEmpty(data.startingWeaponId))
        {
            var allTalents = GameDataMgr.Instance != null ? GameDataMgr.Instance.talentDatas : null;
            if (allTalents != null)
            {
                foreach (var talent in allTalents)
                {
                    if (talent is WeaponTalent weaponTalent && weaponTalent.id == data.startingWeaponId)
                    {
                        weaponTalent.OnAcquire(this, 1);
                        break;
                    }
                }
            }
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
        TalentEffects?.ResetMovementOrigin();
        if (activePlayer == null)
            activePlayer = this;
    }

    private void OnDisable()
    {
        SpecialEffects?.CancelPendingShots();
        SpecialEffects?.ClearVisual();
        if (activePlayer == this)
            activePlayer = null;
    }

    private void Update()
    {
        // 先处理上一帧待发射的回响，再允许本帧的新攻击排队。
        SpecialEffects?.Tick(Time.deltaTime);
        UpdateInvincible();
        UpdateShield();
        UpdateStationary();
        TickReactiveTalents(Time.deltaTime);
        TalentEffects?.Tick(Time.deltaTime);
        DerivedEffects?.Tick(Time.deltaTime);
        AdvancedEffects?.Tick(Time.deltaTime);
        TickHealthRegeneration(Time.deltaTime);

        if (dashBuffTimer > 0f)
            dashBuffTimer -= Time.deltaTime;

        if (damageBuff != null)
            damageBuff.Tick(Time.deltaTime);
        if (doubleAction != null) doubleAction.Tick(Time.deltaTime);

        TickWeapons();
    }

    private void UpdateShield()
    {
        if (SpecialEffects != null && SpecialEffects.PreventShieldDecay) return;
        if (currentShield > 0f && data != null)
        {
            float decay = data.ShieldDecayRate * (1f + GetStatBonus(StatType.ShieldDecay));
            decay = Mathf.Max(0f, decay);
            currentShield = Mathf.Max(0f, currentShield - decay * Time.deltaTime);
        }
    }

    private void UpdateStationary()
    {
        if (stationaryAttackSpeedMax <= 0f && stationarySizeMax <= 0f)
        {
            statBonuses[StatType.StationaryAttackSpeed] = 0f;
            return;
        }

        bool moving = controller != null && controller.MoveInput.sqrMagnitude > 0.001f;

        if (moving)
        {
            stationaryTime = 0f;
        }
        else
        {
            stationaryTime = Mathf.Min(stationaryMaxTime, stationaryTime + Time.deltaTime);
        }

        float t = stationaryTime / stationaryMaxTime;

        statBonuses[StatType.StationaryAttackSpeed] = stationaryAttackSpeedMax * t;

        stationarySizeBonus = stationarySizeMax * t;
        ApplySizeCombined();
    }

    private void TickWeapons()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;
        if (controller != null && (!controller.enabled || controller.IsStunned))
            return;

        float rangeBonus = 1f + GetStatBonus(StatType.AttackRange);

        for (int i = 0; i < weapons.Count; i++)
        {
            WeaponBase weapon = weapons[i];
            if (weapon == null) continue;

            weapon.Tick(Time.deltaTime);

            if (weapon is OrbitWeapon) continue;

            if (!weapon.TickCooldown(Time.deltaTime)) continue;

            float effectiveRange = weapon.range * rangeBonus;

            Enemy target = null;
            if (weapon.RequiresTarget)
            {
                target = FindClosestEnemyInRange(effectiveRange);
                if (target == null) continue;
            }
            else if (weapon.RequiresEnemyInRange)
            {
                if (!HasEnemyInRange(effectiveRange)) continue;
            }

            weapon.Fire(this, target);
            weapon.ResetCooldown(WeaponCooldownMultiplier);
        }
    }

    private bool HasEnemyInRange(float range)
    {
        return FindClosestEnemyInRange(range) != null;
    }

    public void OnSkillInput()
    {
        GameManager manager = GameManager.Instance;
        if (manager == null || manager.IsGameOver) return;
        if (controller != null && controller.IsStunned) return;
        if (skill == null) return;

        float cdMult = 1f + GetStatBonus(StatType.SkillCooldown);
        cdMult = Mathf.Max(0.1f, cdMult);

        if (!manager.TryStartSkillCooldown(this, skill.Cooldown * cdMult)) return;

        skill.Cast(this);
    }

    internal bool CanTargetEnemy(Enemy enemy)
    {
        return enemy != null && !enemy.IsDead && enemy.isActiveAndEnabled &&
            enemy.gameObject.scene == gameObject.scene &&
            (enemyLayers.value & (1 << enemy.gameObject.layer)) != 0;
    }

    internal Enemy FindClosestEnemyInRange(float range)
    {
        Enemy closest = null;
        float closestDistance = range * range;
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        foreach (Enemy enemy in enemies)
        {
            if (!CanTargetEnemy(enemy)) continue;

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
            GameManager.Instance?.RecordAchievementLevel(this);

            SendMessage("OnLevelUp", level, SendMessageOptions.DontRequireReceiver);
            RollTalentChoices();
            break;
        }
    }

    public static void NotifyEnemyDefeated(int reward)
    {
        NotifyEnemyDefeated(reward, null);
    }

    public static void NotifyEnemyDefeated(int reward, Enemy defeatedEnemy)
    {
        if (activePlayer == null || activePlayer.IsDead) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        GameManager.Instance?.RecordAchievementKill(activePlayer);
        activePlayer.AddExperience(reward);
        activePlayer.RegisterFrenzyKill();

        if (activePlayer.killStackBonusPerKill > 0f)
        {
            activePlayer.statBonuses[StatType.KillStackDamage] =
                activePlayer.GetStatBonus(StatType.KillStackDamage) + activePlayer.killStackBonusPerKill;
        }

        float heal = activePlayer.GetStatBonus(StatType.HealOnKill);
        if (heal > 0f)
            activePlayer.Heal(heal);

        float shield = activePlayer.GetStatBonus(StatType.ShieldOnKill);
        if (shield > 0f)
            activePlayer.AddShield(shield);

        activePlayer.TalentEffects?.OnEnemyDefeated(defeatedEnemy);
        activePlayer.AdvancedEffects?.OnEnemyDefeated(defeatedEnemy);
    }

    public void TakeDamage(int dmg)
    {
        if (IsInvincible || dmg <= 0 || IsDead) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        float dodge = Mathf.Clamp01(GetStatBonus(StatType.DodgeChance));
        if (dodge > 0f && Random.value < dodge)
        {
            // 闪避不消耗护盾、不触发受伤天赋；沿用受击间隔，避免接触伤害逐帧重试。
            StartInvincible();
            DerivedEffects?.OnDodge();
            return;
        }

        float reduction = Mathf.Clamp(GetStatBonus(StatType.DamageReduction), 0f, 0.9f);
        float finalDmg = Mathf.Max(1f, dmg * (1f - reduction));

        float remaining = finalDmg;
        bool shieldWasHit = currentShield > 0f;
        bool shieldBroken = currentShield > 0f && remaining >= currentShield;
        float rescueInvincibility = 0f;

        if (currentShield > 0f)
        {
            float absorbed = Mathf.Min(currentShield, remaining);
            currentShield -= absorbed;
            remaining -= absorbed;
        }

        if (remaining > 0f)
        {
            currentHealth = Mathf.Max(0f, currentHealth - remaining);
            if (currentHealth <= 0f)
            {
                if (AdvancedEffects.TryPreventDeath(out float advancedShield, out rescueInvincibility))
                {
                    currentHealth = Mathf.Min(1f, MaxHealth);
                    AddShield(advancedShield);
                }
                else if (TalentEffects.TryPreventDeath(out float rescueShield, out rescueInvincibility))
                {
                    currentHealth = Mathf.Min(1f, MaxHealth);
                    AddShield(rescueShield);
                }
                else
                    OnDeath();
            }
        }

        StartInvincible();
        invincibleTimer = Mathf.Max(invincibleTimer, rescueInvincibility);
        if (shieldBroken && !IsDead)
            TalentEffects.OnShieldBroken();

        AdvancedEffects?.OnDamageTaken(remaining, shieldWasHit);

        // 只有实际损失生命且存活时反击；护盾吸收和无敌帧不会触发。
        if (remaining > 0f && !IsDead)
            TryRetaliation();
    }

    public void AddShield(float amount)
    {
        if (amount <= 0f) return;
        if (SpecialEffects != null && SpecialEffects.ConvertShieldToHealth(amount)) return;
        currentShield += amount;
    }

    public void ClampHealthToMaximum()
    {
        currentHealth = Mathf.Min(currentHealth, MaxHealth);
    }

    public void Heal(float amount)
    {
        if (amount <= 0f || IsDead) return;
        amount *= Mathf.Max(0f, 1f + GetStatBonus(StatType.HealingReceived));
        float restored = Mathf.Min(amount, Mathf.Max(0f, MaxHealth - currentHealth));
        currentHealth += restored;

        float overflow = amount - restored;
        if (overflow > 0f && overhealShieldRatio > 0f)
        {
            // 只限制本次转化的增量，不削减技能或其他天赋已经提供的护盾。
            float room = Mathf.Max(0f, MaxHealth * overhealShieldCapPercent - currentShield);
            AddShield(Mathf.Min(overflow * overhealShieldRatio, room));
        }
        DerivedEffects?.OnHealed(restored);
        AdvancedEffects?.OnHealed(restored);
    }

    private void OnDeath()
    {
        GameManager.Instance?.OnPlayerDeath();
    }

    private void TickHealthRegeneration(float deltaTime)
    {
        if (deltaTime <= 0f || !isActiveAndEnabled || IsDead) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        float regeneration = GetStatBonus(StatType.HealthRegeneration);
        if (regeneration > 0f) Heal(regeneration * deltaTime);
    }

    public float DashDistanceMultiplier => Mathf.Max(0.1f, 1f + GetStatBonus(StatType.DashDistance));

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
            // 每升一级累计增加 10% 基础伤害，独立于天赋和技能倍率。
            float mult = 1f + (level - 1) * 0.1f;
            if (damageBuff != null && damageBuff.IsActive)
                mult *= damageBuff.Multiplier;
            mult *= baseDamageMultiplier;
            mult *= (1f + GetStatBonus(StatType.AttackDamage));
            mult *= (1f + GetStatBonus(StatType.KillStackDamage));

            if (moveSpeedToDamageRatio > 0f)
            {
                float moveBonus = MoveSpeedBonus;
                mult *= (1f + moveBonus * moveSpeedToDamageRatio);
            }

            if (dashBuffTimer > 0f)
                mult *= (1f + dashBuffDamageBonus);

            if (SpecialEffects != null)
                mult = SpecialEffects.ApplyDamageMultiplier(mult);

            return mult;
        }
    }

    public float WeaponCooldownMultiplier
    {
        get
        {
            float mult = 1f;

            if (GameManager.Instance != null && GameManager.Instance.TeamDoubleActionActive)
                mult *= GameManager.Instance.TeamWeaponCooldownMultiplier;
            else if (doubleAction != null && doubleAction.IsActive)
                mult *= doubleAction.WeaponCooldownMultiplier;

            float attackSpeedBonus = GetStatBonus(StatType.AttackSpeed) + GetStatBonus(StatType.StationaryAttackSpeed);
            attackSpeedBonus += frenzyCharges * frenzyAttackSpeedPerCharge;
            mult *= 1f / (1f + attackSpeedBonus);
            mult /= Mathf.Max(1f, AdvancedEffects?.WeaponAttackSpeedMultiplier ?? 1f);
            if (SpecialEffects != null)
                mult *= SpecialEffects.WeaponCooldownSlowMultiplier;

            return mult;
        }
    }

    public float MoveSpeedBonus
    {
        get
        {
            float bonus = GetStatBonus(StatType.MoveSpeed);
            bonus += frenzyCharges * frenzyMoveSpeedPerCharge;
            if (GameManager.Instance != null && GameManager.Instance.TeamDoubleActionActive)
                bonus += GameManager.Instance.TeamMoveSpeedBonus;
            else if (doubleAction != null && doubleAction.IsActive)
                bonus += doubleAction.MoveSpeedBonus;
            return bonus;
        }
    }

    public float DashCooldownMultiplier
    {
        get
        {
            float bonus = GetStatBonus(StatType.DashCooldown);
            return Mathf.Max(0.1f, 1f + bonus);
        }
    }

    public float BulletSpeedMultiplier
    {
        get { return 1f + GetStatBonus(StatType.BulletSpeed); }
    }

    public void SetBaseDamageMultiplier(float mult)
    {
        baseDamageMultiplier = Mathf.Max(0.01f, mult);
    }

    public void AddKillStackBonus(float perKill)
    {
        killStackBonusPerKill += perKill;
    }

    public void SetStationaryBonus(float atkSpeedMax, float sizeMax)
    {
        stationaryAttackSpeedMax = Mathf.Max(stationaryAttackSpeedMax, atkSpeedMax);
        stationarySizeMax = Mathf.Max(stationarySizeMax, sizeMax);
    }

    public void SetDashBuff(float duration, float damageBonus)
    {
        dashBuffDuration = Mathf.Max(dashBuffDuration, duration);
        dashBuffDamageBonus = Mathf.Max(dashBuffDamageBonus, damageBonus);
    }

    public void OnDashPerformed()
    {
        if (IsDead || !isActiveAndEnabled) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        if (dashBuffDuration > 0f)
            dashBuffTimer = dashBuffDuration;
        TryDashPulse();
        DerivedEffects?.OnDash();
        AdvancedEffects?.OnDash();
    }

    private void ResetReactiveTalents()
    {
        dashPulseRadius = dashPulseStunDuration = dashPulseCooldown = dashPulseTimer = 0f;
        dashPulseDamage = 0;
        dashPulseEffectPrefab = null;
        frenzyDuration = frenzyTimer = frenzyAttackSpeedPerCharge = frenzyMoveSpeedPerCharge = 0f;
        frenzyMaxCharges = frenzyCharges = 0;
        overhealShieldRatio = overhealShieldCapPercent = 0f;
        retaliationCount = retaliationDamage = 0;
        retaliationCooldown = retaliationTimer = 0f;
        retaliationPrefab = null;
    }

    private void TickReactiveTalents(float deltaTime)
    {
        dashPulseTimer = Mathf.Max(0f, dashPulseTimer - deltaTime);
        retaliationTimer = Mathf.Max(0f, retaliationTimer - deltaTime);
        frenzyTimer = Mathf.Max(0f, frenzyTimer - deltaTime);
        if (frenzyTimer <= 0f)
            frenzyCharges = 0;
    }

    public void SetKillFrenzy(float duration, int maxCharges, float attackSpeedPerCharge, float moveSpeedPerCharge)
    {
        frenzyDuration = Mathf.Max(0f, duration);
        frenzyMaxCharges = Mathf.Max(0, maxCharges);
        frenzyAttackSpeedPerCharge = Mathf.Max(0f, attackSpeedPerCharge);
        frenzyMoveSpeedPerCharge = Mathf.Max(0f, moveSpeedPerCharge);
        frenzyCharges = Mathf.Min(frenzyCharges, frenzyMaxCharges);
    }

    private void RegisterFrenzyKill()
    {
        if (frenzyDuration <= 0f || frenzyMaxCharges <= 0) return;
        frenzyCharges = Mathf.Min(frenzyCharges + 1, frenzyMaxCharges);
        frenzyTimer = frenzyDuration;
    }

    public void SetOverhealShield(float ratio, float capPercent)
    {
        overhealShieldRatio = Mathf.Max(0f, ratio);
        overhealShieldCapPercent = Mathf.Clamp01(capPercent);
    }

    public void SetDashPulse(float radius, int damage, float stun, float cooldown, GameObject effect)
    {
        dashPulseRadius = Mathf.Max(0f, radius);
        dashPulseDamage = Mathf.Max(0, damage);
        dashPulseStunDuration = Mathf.Max(0f, stun);
        dashPulseCooldown = Mathf.Max(0.05f, cooldown);
        dashPulseEffectPrefab = effect;
    }

    private void TryDashPulse()
    {
        if (dashPulseRadius <= 0f || dashPulseTimer > 0f) return;
        dashPulseTimer = dashPulseCooldown;
        int damage = Mathf.RoundToInt(dashPulseDamage * DamageMultiplier);
        Vector2 origin = transform.position;
        foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (enemy == null || enemy.IsDead || !enemy.isActiveAndEnabled) continue;
            if (enemy.gameObject.scene != gameObject.scene) continue;
            if ((enemyLayers.value & (1 << enemy.gameObject.layer)) == 0) continue;
            if (((Vector2)enemy.transform.position - origin).sqrMagnitude > dashPulseRadius * dashPulseRadius) continue;
            enemy.Stun(dashPulseStunDuration);
            enemy.TakeDamageFrom(damage, transform.position);
        }

        if (dashPulseEffectPrefab != null)
        {
            GameObject effect = Instantiate(dashPulseEffectPrefab, transform.position, Quaternion.identity);
            effect.transform.localScale = Vector3.one * dashPulseRadius * 2f;
            if (effect.TryGetComponent<SpriteRenderer>(out var sprite))
                sprite.color = new Color(0.3f, 0.75f, 1f, 0.3f);
            Destroy(effect, 0.2f);
        }
    }

    public void SetRetaliation(int count, int damage, float cooldown, GameObject prefab)
    {
        retaliationCount = Mathf.Clamp(count, 1, 32);
        retaliationDamage = Mathf.Max(0, damage);
        retaliationCooldown = Mathf.Max(0.05f, cooldown);
        // 与 PiercingWeapon 一样，必须由匹配的子弹组件接收 Init。
        retaliationPrefab = prefab != null && prefab.GetComponent<PiercingBullet>() != null ? prefab : null;
    }

    private void TryRetaliation()
    {
        if (retaliationPrefab == null || retaliationTimer > 0f) return;
        retaliationTimer = retaliationCooldown;
        float damage = retaliationDamage * DamageMultiplier;
        int projectileCount = retaliationCount;
        SpecialEffects.FireProjectileVolley(() =>
        {
            for (int i = 0; i < projectileCount; i++)
            {
                float angle = i * Mathf.PI * 2f / projectileCount;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector3 position = transform.position + (Vector3)(direction * 0.5f);
                GameObject projectile = Instantiate(retaliationPrefab, position, Quaternion.identity);
                projectile.GetComponent<PiercingBullet>().SetTalentOwner(this);
                projectile.GetComponent<PiercingBullet>().Init(direction, damage, ExtraPierce, BulletSpeedMultiplier);
            }
        });
    }

    private void ApplySize()
    {
        baseSizeBonus = GetStatBonus(StatType.SizeIncrease);
        ApplySizeCombined();
    }

    private void ApplySizeCombined()
    {
        float total = baseSizeBonus + stationarySizeBonus;
        float scale = 0.5f * Mathf.Max(0.1f, 1f + total);
        transform.localScale = new Vector3(scale, scale, 1f);
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

            if (weapon is OrbitWeapon orbit) orbit.Refresh(this);
            if (weapon is AuraWeapon aura) aura.Refresh(this);
        }
        else
        {
            existing.OnStackUp(stack);

            if (existing is OrbitWeapon orbit) orbit.Refresh(this);
            if (existing is AuraWeapon aura) aura.Refresh(this);
        }
    }

    public void RefreshOrbitWeapons()
    {
        foreach (WeaponBase weapon in weapons)
            if (weapon is OrbitWeapon orbit) orbit.Refresh(this);
    }

    // ===== 天赋 =====

    public int GetColorCount(TalentColor color)
    {
        return colorCounts.TryGetValue(color, out int count) ? count : 0;
    }

    public bool CanAcquireColor(TalentColor color)
    {
        int limit = color switch
        {
            TalentColor.Red => TalentBase.MaxRed,
            TalentColor.Blue => TalentBase.MaxBlue,
            TalentColor.Green => TalentBase.MaxGreen,
            _ => 0
        };
        return GetColorCount(color) < limit;
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
        if (talent is SpecialTalent special && (SpecialEffects == null || !SpecialEffects.CanAcquire(special))) return false;

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

    public bool ChooseTalent(TalentBase talent)
    {
        if (talent == null) return false;
        if (!pendingChoices.Contains(talent)) return false;

        bool success = TryAddTalent(talent);
        if (success)
        {
            pendingChoices.Clear();
            GameManager.Instance?.RecordAchievementTalent(this);
        }

        return success;
    }

    public bool HasPrerequisites(TalentBase talent)
    {
        if (talent == null || talent.prerequisiteIds == null || talent.prerequisiteIds.Length == 0)
            return true;

        foreach (var reqId in talent.prerequisiteIds)
        {
            if (string.IsNullOrEmpty(reqId)) continue;

            bool found = acquiredTalents.Exists(t => t.data != null && t.data.id == reqId);
            if (!found) return false;
        }

        return true;
    }

    private void RollTalentChoices()
    {
        pendingChoices.Clear();

        var allTalents = GameDataMgr.Instance != null ? GameDataMgr.Instance.talentDatas : null;
        if (allTalents == null || allTalents.Count == 0) return;

        List<TalentBase> candidates = new();
        foreach (var talent in allTalents)
        {
            if (talent == null) continue;
            if (!CanAcquireColor(talent.color)) continue;
            if (talent is SpecialTalent special && (SpecialEffects == null || !SpecialEffects.CanAcquire(special))) continue;

            if (!HasPrerequisites(talent)) continue;

            if (talent is SkillModTalent skillMod)
            {
                if (skill == null || skill.Id != skillMod.targetSkillId)
                    continue;
            }

            int stack = GetTalentStack(talent);
            if (stack >= talent.maxStack) continue;

            candidates.Add(talent);
        }

        if (candidates.Count == 0) return;

        int choiceCount = baseChoicesPerLevel + Mathf.RoundToInt(GetStatBonus(StatType.ExtraChoice));

        for (int i = 0; i < choiceCount && candidates.Count > 0; i++)
        {
            TalentBase picked = PickWeighted(candidates);
            if (picked == null) break;
            pendingChoices.Add(picked);
            candidates.Remove(picked);
        }

        if (pendingChoices.Count == 0) return;

        TalentPanel panel = UIManager.Instance.ShowPanel<TalentPanel>();
        panel.Show(new List<TalentBase>(pendingChoices), selected =>
        {
            ChooseTalent(selected);
            UIManager.Instance.HidePanel<TalentPanel>(false);
        });
    }

    private TalentBase PickWeighted(List<TalentBase> candidates)
    {
        int total = 0;
        foreach (var c in candidates)
            total += Mathf.Max(1, c.weight);

        if (total <= 0) return null;

        int roll = Random.Range(0, total);
        foreach (var c in candidates)
        {
            roll -= Mathf.Max(1, c.weight);
            if (roll < 0) return c;
        }
        return candidates[candidates.Count - 1];
    }

    // ===== 属性加成 =====

    public float GetStatBonus(StatType type)
    {
        return statBonuses.TryGetValue(type, out float v) ? v : 0f;
    }

    public void ApplyStatTalent(StatType type, float value, int stack)
    {
        float previousMaxHealth = UnlockedMaxHealth;
        statBonuses[type] = GetStatBonus(type) + value;

        if (type == StatType.SizeIncrease)
            ApplySize();
        if (type == StatType.MaxHealth)
            SpecialEffects?.OnMaxHealthIncreased(UnlockedMaxHealth - previousMaxHealth);
    }
    public void SetMoveSpeedToDamageRatio(float ratio)
    {
    moveSpeedToDamageRatio = Mathf.Max(moveSpeedToDamageRatio, ratio);
    }
}
