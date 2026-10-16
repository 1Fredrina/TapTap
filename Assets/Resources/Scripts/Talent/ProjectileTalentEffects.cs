using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 普通/贯穿抗体共用的基础天赋。发射时快照属性，每枚子弹独立保存命中与弹射状态。
/// 不改变原 Init 接口，也不依赖 ActivePlayer，避免切换角色后错误继承天赋。
/// </summary>
public sealed class ProjectileTalentEffects
{
    private readonly Player owner;
    private readonly HashSet<Enemy> hitEnemies = new();
    private readonly float criticalChance, turnSpeed;
    private int ricochets;
    private Enemy homingTarget;
    private float searchTimer;
    private readonly DerivedTalentEffects derived;
    private readonly AdvancedTalentEffects advanced;
    private readonly DerivedTalent matureExplosion;
    private readonly float ricochetDamageBonus;
    private readonly ProjectileAugmentState advancedState;
    private int completedRicochets;
    private float age;
    private bool exploded;
    private const float SeekRadius = 6f;

    public float LifetimeMultiplier { get; }
    public float SizeMultiplier { get; }
    public int BonusPierce => advanced?.GetBonusPierce(advancedState) ?? 0;

    public ProjectileTalentEffects(Player owner)
    {
        this.owner = owner;
        derived = owner != null ? owner.DerivedEffects : null;
        advanced = owner != null ? owner.AdvancedEffects : null;
        matureExplosion = derived?.GetConfig(DerivedEffect.MatureExplosion);
        ricochetDamageBonus = Mathf.Max(0f, derived?.GetConfig(DerivedEffect.RicochetHunt)?.value ?? 0f);
        advancedState = advanced?.CreateProjectileState() ?? new ProjectileAugmentState();
        float Bonus(StatType type) => owner != null ? owner.GetStatBonus(type) : 0f;
        criticalChance = Mathf.Clamp01(Bonus(StatType.ProjectileCriticalChance));
        turnSpeed = Mathf.Max(0f, Bonus(StatType.ProjectileHoming));
        ricochets = Mathf.Max(0, Mathf.RoundToInt(Bonus(StatType.ProjectileRicochet)));
        LifetimeMultiplier = Mathf.Max(0.1f, 1f + Bonus(StatType.ProjectileLifetime));
        SizeMultiplier = Mathf.Max(0.1f, 1f + Bonus(StatType.ProjectileSize));
    }

    public bool TryHit(Enemy enemy, int damage, Vector2 sourcePosition)
    {
        if (enemy == null || enemy.IsDead || !enemy.isActiveAndEnabled) return false;
        if (owner != null && !owner.CanTargetEnemy(enemy)) return false;
        // 一个敌人的多个碰撞体、转向后再次穿过同一敌人，都只造成一次伤害。
        if (!hitEnemies.Add(enemy)) return false;
        damage = Mathf.Max(0, Mathf.RoundToInt(damage * advancedState.damageMultiplier * (1f + completedRicochets * ricochetDamageBonus)));
        advanced?.OnProjectileHit(enemy, advancedState, ref damage);
        bool derivedCritical = derived?.TryConsumeCritical() ?? false;
        bool critical = advancedState.guaranteedCritical || derivedCritical || (criticalChance > 0f && Random.value < criticalChance);
        if (critical) damage *= 2;
        Vector2 impactPosition = enemy.transform.position;
        enemy.TakeDamageFrom(damage, sourcePosition);
        advanced?.OnProjectileResolved(enemy, advancedState, damage);
        if (critical && damage > 0) derived?.OnProjectileCritical();
        if (!exploded && matureExplosion != null && age >= matureExplosion.threshold && damage > 0)
        {
            exploded = true;
            derived.ExplodeProjectile(matureExplosion, impactPosition, damage, enemy);
        }
        return true;
    }

    public Vector2 Steer(Vector2 position, Vector2 direction, float deltaTime)
    {
        age += Mathf.Max(0f, deltaTime);
        if (turnSpeed <= 0f || deltaTime <= 0f) return direction;
        searchTimer -= deltaTime;
        if (!CanSeek(homingTarget, position)) homingTarget = null;
        if (homingTarget == null && searchTimer <= 0f)
        {
            homingTarget = FindTarget(position);
            searchTimer = 0.1f;
        }
        if (homingTarget == null) return direction;
        Vector2 desired = (Vector2)homingTarget.transform.position - position;
        if (desired.sqrMagnitude < 0.0001f) return direction;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float targetAngle = Mathf.Atan2(desired.y, desired.x) * Mathf.Rad2Deg;
        angle = Mathf.MoveTowardsAngle(angle, targetAngle, turnSpeed * deltaTime) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    public bool TryRicochet(Vector2 position, out Vector2 direction)
    {
        direction = Vector2.zero;
        if (ricochets <= 0) return false;
        Enemy target = FindTarget(position);
        if (target == null) return false;
        direction = ((Vector2)target.transform.position - position).normalized;
        if (direction.sqrMagnitude < 0.0001f) direction = Vector2.right;
        ricochets--;
        completedRicochets++;
        homingTarget = target;
        return true;
    }

    private bool CanSeek(Enemy enemy, Vector2 position) => owner != null && owner.CanTargetEnemy(enemy) &&
        !hitEnemies.Contains(enemy) && ((Vector2)enemy.transform.position - position).sqrMagnitude <= SeekRadius * SeekRadius;

    private Enemy FindTarget(Vector2 position)
    {
        if (owner == null) return null;
        Enemy closest = null;
        float bestDistance = SeekRadius * SeekRadius;
        foreach (Enemy enemy in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (!CanSeek(enemy, position)) continue;
            float distance = ((Vector2)enemy.transform.position - position).sqrMagnitude;
            if (distance > bestDistance) continue;
            bestDistance = distance;
            closest = enemy;
        }
        return closest;
    }
}
