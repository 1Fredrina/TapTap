using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 角色独有的触发型天赋状态。天赋仍通过 OnAcquire 配置；Player 只转发生命周期和战斗通知。
/// 不把计时器写入共享的 TalentBase，也不订阅全局事件，重新 Initialize 即可清空。
/// </summary>
public sealed class PlayerTalentEffects
{
    private readonly Player owner;
    private Vector2 lastPosition;
    private float travelDistance, distancePerVolley, travelRange, travelSpread;
    private int travelDamage, travelProjectileCount;
    private GameObject travelProjectile;
    private float targetRetryTimer;

    private float shieldBreakRadius, shieldBreakStun, shieldBreakCooldown, shieldBreakTimer;
    private int shieldBreakDamage;
    private GameObject shieldBreakEffect;

    private int killsPerNova, novaKills, novaDamage;
    private float novaRadius;
    private bool resolvingNova;
    private GameObject novaEffect;

    private float lastStandCooldown, lastStandTimer, lastStandShieldPercent, lastStandInvincibility;
    private GameObject lastStandEffect;

    public float TravelCharge => distancePerVolley > 0f ? Mathf.Clamp01(travelDistance / distancePerVolley) : 0f;
    public int NovaKills => novaKills;
    public int KillsPerNova => killsPerNova;
    public float LastStandCooldownRemaining => lastStandTimer;
    public bool HasLastStand => lastStandCooldown > 0f;

    private bool CanAct => owner != null && owner.isActiveAndEnabled && !owner.IsDead &&
        (GameManager.Instance == null || !GameManager.Instance.IsGameOver);

    public PlayerTalentEffects(Player owner)
    {
        this.owner = owner;
        ResetMovementOrigin();
    }

    public void ResetMovementOrigin() => lastPosition = owner.transform.position;

    public void ConfigureTravel(float distance, float range, int count, float spread, int damage, GameObject prefab)
    {
        distancePerVolley = Mathf.Max(0.1f, distance);
        travelRange = Mathf.Max(0f, range);
        travelProjectileCount = Mathf.Clamp(count, 1, 32);
        travelSpread = Mathf.Clamp(spread, 0f, 180f);
        travelDamage = Mathf.Max(0, damage);
        travelProjectile = prefab != null && prefab.GetComponent<PiercingBullet>() != null ? prefab : null;
        travelDistance = Mathf.Min(travelDistance, distancePerVolley);
        ResetMovementOrigin();
    }

    public void ConfigureShieldBreak(float radius, int damage, float stun, float cooldown, GameObject effect)
    {
        shieldBreakRadius = Mathf.Max(0f, radius);
        shieldBreakDamage = Mathf.Max(0, damage);
        shieldBreakStun = Mathf.Max(0f, stun);
        shieldBreakCooldown = Mathf.Max(0.05f, cooldown);
        shieldBreakEffect = effect;
    }

    public void ConfigureDeathNova(int kills, float radius, int damage, GameObject effect)
    {
        killsPerNova = Mathf.Max(1, kills);
        novaRadius = Mathf.Max(0f, radius);
        novaDamage = Mathf.Max(0, damage);
        novaEffect = effect;
    }

    public void ConfigureLastStand(float cooldown, float shieldPercent, float invincibility, GameObject effect)
    {
        lastStandCooldown = Mathf.Max(0.05f, cooldown);
        lastStandShieldPercent = Mathf.Clamp01(shieldPercent);
        lastStandInvincibility = Mathf.Max(0f, invincibility);
        lastStandEffect = effect;
    }

    public void Tick(float deltaTime)
    {
        Vector2 position = owner.transform.position;
        float distance = Vector2.Distance(lastPosition, position);
        lastPosition = position;
        if (!CanAct || deltaTime <= 0f) return;

        shieldBreakTimer = Mathf.Max(0f, shieldBreakTimer - deltaTime);
        lastStandTimer = Mathf.Max(0f, lastStandTimer - deltaTime);
        targetRetryTimer = Mathf.Max(0f, targetRetryTimer - deltaTime);
        if (travelProjectile == null) return;
        if (owner.Controller != null && (!owner.Controller.enabled || owner.Controller.IsStunned)) return;

        // 按实际位移充能，顶墙和仅按住移动键无效；单帧跨越整个充能距离视作传送。
        if (distance < distancePerVolley)
            travelDistance = Mathf.Min(distancePerVolley, travelDistance + distance);
        if (travelDistance < distancePerVolley || targetRetryTimer > 0f) return;

        // 没有目标时保留一轮满蓄能，限制空场时的目标查询频率。
        targetRetryTimer = 0.1f;
        Enemy target = owner.FindClosestEnemyInRange(travelRange * (1f + owner.GetStatBonus(StatType.AttackRange)));
        if (target == null) return;
        travelDistance = 0f;
        Vector2 aim = ((Vector2)target.transform.position - position).normalized;
        if (aim.sqrMagnitude < 0.001f) aim = Vector2.right;
        float baseAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
        float damage = travelDamage * owner.DamageMultiplier;
        int baseProjectileCount = travelProjectileCount;
        owner.SpecialEffects.FireProjectileVolley(() =>
        {
            for (int i = 0; i < baseProjectileCount; i++)
            {
                float angle = (baseAngle + (i - (baseProjectileCount - 1) * 0.5f) * travelSpread) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                GameObject projectile = Spawn(travelProjectile, (Vector2)owner.transform.position + direction * 0.5f);
                projectile.GetComponent<PiercingBullet>().SetTalentOwner(owner);
                projectile.GetComponent<PiercingBullet>().Init(direction, damage, owner.ExtraPierce, owner.BulletSpeedMultiplier);
            }
        });
    }

    public void OnShieldBroken()
    {
        if (!CanAct || shieldBreakRadius <= 0f || shieldBreakTimer > 0f) return;
        shieldBreakTimer = shieldBreakCooldown;
        Pulse(owner.transform.position, shieldBreakRadius, shieldBreakDamage, shieldBreakStun,
            shieldBreakEffect, new Color(0.35f, 0.8f, 1f, 0.4f));
    }

    public void OnEnemyDefeated(Enemy enemy)
    {
        if (!CanAct || killsPerNova <= 0 || resolvingNova || enemy == null || !enemy.IsDead) return;
        if (enemy.gameObject.scene != owner.gameObject.scene) return;
        if ((owner.EnemyLayers.value & (1 << enemy.gameObject.layer)) == 0) return;
        if (++novaKills < killsPerNova) return;
        novaKills = 0;

        // 爆炸击杀照常发经验和击杀收益，但不再次积累本天赋，避免递归爆炸。
        resolvingNova = true;
        try
        {
            Pulse(enemy.transform.position, novaRadius, novaDamage, 0f, novaEffect,
                new Color(1f, 0.35f, 0.55f, 0.4f));
        }
        finally
        {
            resolvingNova = false;
        }
    }

    // 由 Player 在扣血归零、进入死亡流程之前调用，此时不能使用包含 IsDead 的 CanAct。
    public bool TryPreventDeath(out float shield, out float invincibility)
    {
        shield = invincibility = 0f;
        if (owner == null || !owner.isActiveAndEnabled || lastStandCooldown <= 0f || lastStandTimer > 0f) return false;
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return false;
        lastStandTimer = lastStandCooldown;
        shield = owner.MaxHealth * lastStandShieldPercent;
        invincibility = lastStandInvincibility;
        ShowPulse(owner.transform.position, 1.25f, lastStandEffect, new Color(0.4f, 1f, 0.65f, 0.5f));
        return true;
    }

    private void Pulse(Vector2 origin, float radius, int baseDamage, float stun, GameObject effect, Color color)
    {
        int damage = Mathf.RoundToInt(baseDamage * owner.DamageMultiplier);
        foreach (Enemy enemy in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (!owner.CanTargetEnemy(enemy)) continue;
            if (((Vector2)enemy.transform.position - origin).sqrMagnitude > radius * radius) continue;
            enemy.Stun(stun);
            enemy.TakeDamageFrom(damage, owner.transform.position);
        }
        ShowPulse(origin, radius, effect, color);
    }

    private void ShowPulse(Vector2 origin, float radius, GameObject prefab, Color color)
    {
        if (prefab == null) return;
        GameObject effect = Spawn(prefab, origin);
        effect.transform.localScale = Vector3.one * radius * 2f;
        if (effect.TryGetComponent<SpriteRenderer>(out var sprite)) sprite.color = color;
        Object.Destroy(effect, 0.25f);
    }

    private GameObject Spawn(GameObject prefab, Vector2 position)
    {
        GameObject instance = Object.Instantiate(prefab, new Vector3(position.x, position.y, owner.transform.position.z), Quaternion.identity);
        SceneManager.MoveGameObjectToScene(instance, owner.gameObject.scene);
        return instance;
    }
}
