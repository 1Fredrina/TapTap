using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>衍生天赋的角色运行时；重新 Initialize 会使旧子弹持有的运行时失效。</summary>
public sealed class DerivedTalentEffects
{
    private readonly Player owner;
    private readonly Dictionary<DerivedEffect, DerivedTalent> configs = new();
    private readonly Dictionary<DerivedEffect, GameObject> visuals = new();
    private float healed, pulseTimer, dashTimer, recoveryTimer, leechTimer, insightTimer;
    private int insightCharges;
    private bool resolvingPulse;

    private bool CanAct => owner != null && owner.DerivedEffects == this &&
        owner.isActiveAndEnabled && !owner.IsDead &&
        (GameManager.Instance == null || !GameManager.Instance.IsGameOver);

    public DerivedTalentEffects(Player owner) => this.owner = owner;

    public void Configure(DerivedTalent talent)
    {
        configs[talent.effect] = talent;
        if (!string.IsNullOrEmpty(talent.effectPrefabPath))
            visuals[talent.effect] = Resources.Load<GameObject>(talent.effectPrefabPath);
    }

    public DerivedTalent GetConfig(DerivedEffect effect) =>
        configs.TryGetValue(effect, out var config) ? config : null;

    public void Tick(float deltaTime)
    {
        if (!CanAct || deltaTime <= 0f) return;
        pulseTimer = Mathf.Max(0f, pulseTimer - deltaTime);
        dashTimer = Mathf.Max(0f, dashTimer - deltaTime);
        recoveryTimer = Mathf.Max(0f, recoveryTimer - deltaTime);
        leechTimer = Mathf.Max(0f, leechTimer - deltaTime);
        insightTimer = Mathf.Max(0f, insightTimer - deltaTime);
        if (insightTimer <= 0f) insightCharges = 0;
        TryHealingPulse();
    }

    public void OnHealed(float restored)
    {
        var config = GetConfig(DerivedEffect.RegenerationPulse);
        if (!CanAct || config == null || restored <= 0f || resolvingPulse) return;
        // 最多储存一轮，溢出治疗和本次脉冲带来的击杀治疗不充能。
        healed = Mathf.Min(Mathf.Max(0.01f, config.threshold), healed + restored);
        TryHealingPulse();
    }

    private void TryHealingPulse()
    {
        var config = GetConfig(DerivedEffect.RegenerationPulse);
        if (config == null || resolvingPulse || pulseTimer > 0f || healed < Mathf.Max(0.01f, config.threshold)) return;
        healed = 0f;
        pulseTimer = Mathf.Max(0.05f, config.cooldown);
        resolvingPulse = true;
        try { Pulse(config, owner.transform.position, Mathf.RoundToInt(config.damage * owner.DamageMultiplier)); }
        finally { resolvingPulse = false; }
    }

    public void OnDodge()
    {
        var config = GetConfig(DerivedEffect.DodgeInsight);
        if (!CanAct || config == null) return;
        insightCharges = Mathf.Max(0, Mathf.RoundToInt(config.value));
        insightTimer = Mathf.Max(0f, config.duration);
    }

    public bool TryConsumeCritical()
    {
        if (!CanAct || insightTimer <= 0f || insightCharges <= 0) return false;
        insightCharges--;
        return true;
    }

    public void OnDash()
    {
        var config = GetConfig(DerivedEffect.DashRepair);
        if (!CanAct || config == null || dashTimer > 0f) return;
        dashTimer = Mathf.Max(0.05f, config.cooldown);
        owner.Heal(Mathf.Max(0f, owner.MaxHealth - owner.CurrentHealth) * Mathf.Clamp01(config.value));
    }

    public void OnStunRecovered()
    {
        var config = GetConfig(DerivedEffect.RecoveryShock);
        if (!CanAct || config == null || recoveryTimer > 0f) return;
        recoveryTimer = Mathf.Max(0.05f, config.cooldown);
        Pulse(config, owner.transform.position, Mathf.RoundToInt(config.damage * owner.DamageMultiplier), null, config.duration);
    }

    public void OnProjectileCritical()
    {
        var config = GetConfig(DerivedEffect.CriticalLeech);
        if (!CanAct || config == null || leechTimer > 0f) return;
        leechTimer = Mathf.Max(0.05f, config.cooldown);
        owner.Heal(Mathf.Max(0f, config.value));
    }

    public void ExplodeProjectile(DerivedTalent config, Vector2 position, int hitDamage, Enemy directVictim)
    {
        if (!CanAct || config == null) return;
        // 命中伤害已经包含角色加成和暴击，爆炸不再重复乘算，也不触发子弹命中事件。
        Pulse(config, position, Mathf.RoundToInt(hitDamage * Mathf.Max(0f, config.value)), directVictim);
    }

    private void Pulse(DerivedTalent config, Vector2 origin, int damage, Enemy excluded = null, float stun = 0f)
    {
        float radius = Mathf.Max(0f, config.range);
        foreach (Enemy enemy in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (enemy == excluded || !owner.CanTargetEnemy(enemy)) continue;
            if (((Vector2)enemy.transform.position - origin).sqrMagnitude > radius * radius) continue;
            if (stun > 0f) enemy.Stun(stun);
            if (damage > 0) enemy.TakeDamageFrom(damage, owner.transform.position);
        }
        if (!visuals.TryGetValue(config.effect, out var prefab) || prefab == null) return;
        GameObject effect = Object.Instantiate(prefab, origin, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(effect, owner.gameObject.scene);
        effect.transform.localScale = Vector3.one * radius * 2f;
        if (effect.TryGetComponent<SpriteRenderer>(out var sprite)) sprite.color = new Color(0.6f, 1f, 0.7f, 0.4f);
        Object.Destroy(effect, 0.25f);
    }
}
