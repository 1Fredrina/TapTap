using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>基础事件与终端天赋的角色专属状态。共享天赋配置不保存战斗计时状态。</summary>
public sealed class AdvancedTalentEffects
{
    private readonly Player owner;
    private readonly Dictionary<AdvancedEffect, AdvancedTalent> configs = new();
    private readonly Dictionary<AdvancedEffect, GameObject> visuals = new();
    private float stationaryTime, bankTimer, apexTimer, singularityTimer;
    private float dashPulseTimer, thornsTimer, apexCooldown, singularityCooldown, deathTimer, overdriveTimer, overdriveCooldown;
    private float singularityHealing;
    private int bankKills, apexKills, singularityKills, ammoHits, overdriveKills, huntKills, huntCharges;
    private bool echoReady, lowHealthReady, phaseReady, stunReady, dashReady, resolving;
    private bool ammoBurstReady;

    private bool CanAct => owner != null && owner.AdvancedEffects == this && owner.isActiveAndEnabled &&
        !owner.IsDead && (GameManager.Instance == null || !GameManager.Instance.IsGameOver);

    public float WeaponAttackSpeedMultiplier
    {
        get
        {
            var config = Get(AdvancedEffect.ArsenalOverdrive);
            return overdriveTimer > 0f && config != null ? Mathf.Max(1f, config.value) : 1f;
        }
    }

    public AdvancedTalentEffects(Player owner) => this.owner = owner;

    public void Configure(AdvancedTalent talent, int stack)
    {
        var configured = new AdvancedTalent
        {
            id = talent.id,
            displayName = talent.displayName,
            description = talent.description,
            color = talent.color,
            maxStack = talent.maxStack,
            weight = talent.weight,
            prerequisiteIds = talent.prerequisiteIds,
            effect = talent.effect,
            value = talent.value,
            threshold = talent.threshold,
            duration = talent.duration,
            range = talent.range,
            cooldown = talent.cooldown,
            window = talent.window,
            healingThreshold = talent.healingThreshold,
            damage = talent.damage,
            terminal = talent.terminal,
            effectPrefabPath = talent.effectPrefabPath
        };
        int extraRanks = Mathf.Max(0, stack - 1);
        switch (talent.effect)
        {
            case AdvancedEffect.DamageEcho: configured.value += 0.5f * extraRanks; break;
            case AdvancedEffect.DashBloom: configured.value += 0.2f * extraRanks; break;
            case AdvancedEffect.KillBank:
                configured.value += 0.05f * extraRanks;
                configured.threshold = Mathf.Max(2f, configured.threshold - extraRanks);
                break;
            case AdvancedEffect.StillnessCharge:
                configured.value += 0.25f * extraRanks;
                configured.threshold = Mathf.Max(0.5f, configured.threshold - 0.5f * extraRanks);
                break;
            case AdvancedEffect.LowHealthBurst: configured.value += 0.2f * extraRanks; break;
            case AdvancedEffect.StunBattery:
                configured.value += 0.25f * extraRanks;
                configured.duration += 0.25f * extraRanks;
                break;
            case AdvancedEffect.ShieldThorns:
                configured.damage *= Mathf.Max(1, stack);
                configured.range += 0.25f * extraRanks;
                configured.cooldown = Mathf.Max(1f, configured.cooldown - 0.5f * extraRanks);
                break;
            case AdvancedEffect.PhaseShift: configured.value *= Mathf.Max(1, stack); break;
            case AdvancedEffect.ExecutionMark:
                configured.value += 0.5f * extraRanks;
                configured.threshold = Mathf.Min(0.5f, configured.threshold + 0.05f * extraRanks);
                break;
            case AdvancedEffect.AmmoCycle:
                configured.value += 0.1f * extraRanks;
                configured.threshold = Mathf.Max(2f, configured.threshold - extraRanks);
                break;
        }
        configs[talent.effect] = configured;
        if (!string.IsNullOrEmpty(talent.effectPrefabPath))
            visuals[talent.effect] = Resources.Load<GameObject>(configured.effectPrefabPath);
    }

    private AdvancedTalent Get(AdvancedEffect effect) => configs.TryGetValue(effect, out var value) ? value : null;

    public void Tick(float deltaTime)
    {
        if (!CanAct || deltaTime <= 0f) return;
        dashPulseTimer = Mathf.Max(0f, dashPulseTimer - deltaTime);
        thornsTimer = Mathf.Max(0f, thornsTimer - deltaTime);
        apexCooldown = Mathf.Max(0f, apexCooldown - deltaTime);
        singularityCooldown = Mathf.Max(0f, singularityCooldown - deltaTime);
        deathTimer = Mathf.Max(0f, deathTimer - deltaTime);
        overdriveTimer = Mathf.Max(0f, overdriveTimer - deltaTime);
        overdriveCooldown = Mathf.Max(0f, overdriveCooldown - deltaTime);
        bankTimer = TickStreak(bankTimer, deltaTime, ref bankKills);
        apexTimer = TickStreak(apexTimer, deltaTime, ref apexKills);
        singularityTimer = TickSingularityWindow(singularityTimer, deltaTime);

        bool moving = owner.Controller != null && owner.Controller.MoveInput.sqrMagnitude > 0.001f;
        stationaryTime = moving ? 0f : Mathf.Min(3f, stationaryTime + deltaTime);
    }

    private static float TickStreak(float timer, float deltaTime, ref int count)
    {
        if (timer <= 0f) { count = 0; return 0f; }
        timer = Mathf.Max(0f, timer - deltaTime);
        if (timer <= 0f) count = 0;
        return timer;
    }

    private float TickSingularityWindow(float timer, float deltaTime)
    {
        if (timer <= 0f)
        {
            singularityKills = 0;
            singularityHealing = 0f;
            return 0f;
        }
        timer = Mathf.Max(0f, timer - deltaTime);
        if (timer <= 0f)
        {
            singularityKills = 0;
            singularityHealing = 0f;
        }
        return timer;
    }

    public void OnHealed(float restored)
    {
        if (!CanAct || resolving || restored <= 0f) return;
        var singularity = Get(AdvancedEffect.BioSingularity);
        if (singularity == null) return;
        if (singularityTimer <= 0f) singularityTimer = Mathf.Max(0.05f, singularity.window);
        singularityHealing = Mathf.Min(Mathf.Max(0f, singularity.healingThreshold), singularityHealing + restored);
        TryBioSingularity(null);
    }

    public void OnDamageTaken(float healthDamage, bool shieldWasHit)
    {
        if (!CanAct || (healthDamage <= 0f && !shieldWasHit)) return;
        if (healthDamage > 0f)
        {
            if (Get(AdvancedEffect.DamageEcho) != null) echoReady = true;
            if (Get(AdvancedEffect.PhaseShift) != null) phaseReady = true;
            var lowHealth = Get(AdvancedEffect.LowHealthBurst);
            if (lowHealth != null && owner.CurrentHealth <= owner.MaxHealth * Mathf.Clamp01(lowHealth.threshold))
                lowHealthReady = true;
        }

        var thorns = Get(AdvancedEffect.ShieldThorns);
        if (thorns != null && shieldWasHit && thornsTimer <= 0f)
        {
            thornsTimer = Mathf.Max(0.05f, thorns.cooldown);
            Pulse(thorns, owner.transform.position, Mathf.RoundToInt(thorns.damage * owner.DamageMultiplier), null, 0f);
        }
    }

    public void OnDash()
    {
        var config = Get(AdvancedEffect.DashBloom);
        if (!CanAct || config == null || dashPulseTimer > 0f) return;
        dashPulseTimer = Mathf.Max(0.05f, config.cooldown);
        dashReady = true;
    }

    public void OnEnemyDefeated(Enemy enemy)
    {
        if (!CanAct || resolving || enemy == null) return;

        var bank = Get(AdvancedEffect.KillBank);
        if (bank != null)
        {
            if (bankKills == 0) bankTimer = Mathf.Max(0.05f, bank.window);
            bankKills++;
            if (bankKills >= Mathf.Max(1, Mathf.RoundToInt(bank.threshold)))
            {
                bankKills = 0;
                bankTimer = 0f;
                owner.AddShield(owner.MaxHealth * Mathf.Max(0f, bank.value));
            }
        }

        var apex = Get(AdvancedEffect.ApexProtocol);
        if (apex != null && apexCooldown <= 0f)
        {
            if (apexKills == 0) apexTimer = Mathf.Max(0.05f, apex.window);
            if (++apexKills >= Mathf.Max(1, Mathf.RoundToInt(apex.threshold)))
            {
                apexKills = 0;
                apexTimer = 0f;
                apexCooldown = Mathf.Max(0.05f, apex.cooldown);
                ResolvePulse(apex, enemy.transform.position);
            }
        }

        var singularity = Get(AdvancedEffect.BioSingularity);
        if (singularity != null)
        {
            if (singularityKills == 0 && singularityTimer <= 0f) singularityTimer = Mathf.Max(0.05f, singularity.window);
            singularityKills++;
            TryBioSingularity(enemy);
        }

        var arsenal = Get(AdvancedEffect.ArsenalOverdrive);
        if (arsenal != null && overdriveCooldown <= 0f)
        {
            overdriveKills++;
            if (overdriveKills >= Mathf.Max(1, Mathf.RoundToInt(arsenal.threshold)))
            {
                overdriveKills = 0;
                overdriveTimer = Mathf.Max(0f, arsenal.duration);
                overdriveCooldown = Mathf.Max(0f, arsenal.cooldown);
            }
        }

        var hunt = Get(AdvancedEffect.PerfectHunt);
        if (hunt != null)
        {
            huntKills++;
            if (huntKills >= Mathf.Max(1, Mathf.RoundToInt(hunt.threshold)))
            {
                huntKills = 0;
                huntCharges = Mathf.Max(1, Mathf.RoundToInt(hunt.value));
            }
        }
    }

    private void TryBioSingularity(Enemy location)
    {
        var config = Get(AdvancedEffect.BioSingularity);
        if (config == null || singularityKills < Mathf.Max(1, Mathf.RoundToInt(config.threshold)) ||
            singularityHealing < config.healingThreshold || singularityCooldown > 0f || resolving) return;

        singularityKills = 0;
        singularityTimer = 0f;
        singularityHealing = 0f;
        singularityCooldown = Mathf.Max(0.05f, config.cooldown);
        Vector2 origin = location != null ? (Vector2)location.transform.position : owner.transform.position;
        resolving = true;
        try
        {
            Pulse(config, origin, Mathf.RoundToInt(config.damage * owner.DamageMultiplier), location, config.duration);
            owner.Heal(owner.MaxHealth * Mathf.Clamp01(config.value));
        }
        finally { resolving = false; }
    }

    private void ResolvePulse(AdvancedTalent config, Vector2 position)
    {
        resolving = true;
        try { Pulse(config, position, Mathf.RoundToInt(config.damage * owner.DamageMultiplier), null, config.duration); }
        finally { resolving = false; }
    }

    public void OnStunRecovered()
    {
        if (CanAct && Get(AdvancedEffect.StunBattery) != null) stunReady = true;
    }

    public bool TryPreventDeath(out float shield, out float invincibility)
    {
        shield = invincibility = 0f;
        var config = Get(AdvancedEffect.ImmortalCycle);
        if (owner == null || !owner.isActiveAndEnabled ||
            (GameManager.Instance != null && GameManager.Instance.IsGameOver) || config == null || deathTimer > 0f)
            return false;
        deathTimer = Mathf.Max(0.05f, config.cooldown);
        shield = owner.MaxHealth * Mathf.Clamp01(config.value);
        invincibility = Mathf.Max(0f, config.duration);
        return true;
    }

    public ProjectileAugmentState CreateProjectileState()
    {
        var state = new ProjectileAugmentState();
        if (!CanAct) return state;

        if (echoReady)
        {
            state.damageMultiplier *= Mathf.Max(1f, Get(AdvancedEffect.DamageEcho).value);
            echoReady = false;
        }
        var stillness = Get(AdvancedEffect.StillnessCharge);
        if (stillness != null && stationaryTime >= Mathf.Max(0f, stillness.threshold))
        {
            state.damageMultiplier *= Mathf.Max(1f, stillness.value);
            stationaryTime = 0f;
        }
        if (phaseReady)
        {
            state.bonusPierce += Mathf.Max(0, Mathf.RoundToInt(Get(AdvancedEffect.PhaseShift).value));
            phaseReady = false;
        }
        var dash = Get(AdvancedEffect.DashBloom);
        if (dashReady && dash != null && state.SetBurst(dash, dash.value))
        {
            dashReady = false;
        }
        var lowHealth = Get(AdvancedEffect.LowHealthBurst);
        if (lowHealthReady && lowHealth != null && owner.CurrentHealth <= owner.MaxHealth * Mathf.Clamp01(lowHealth.threshold))
        {
            if (state.SetBurst(lowHealth, lowHealth.value)) lowHealthReady = false;
        }
        if (stunReady)
        {
            var stun = Get(AdvancedEffect.StunBattery);
            state.damageMultiplier *= Mathf.Max(1f, stun.value);
            state.stunDuration = Mathf.Max(0f, stun.duration);
            stunReady = false;
        }
        var arsenal = Get(AdvancedEffect.ArsenalOverdrive);
        if (arsenal != null && overdriveTimer > 0f) state.damageMultiplier *= Mathf.Max(1f, arsenal.value);
        var hunt = Get(AdvancedEffect.PerfectHunt);
        if (huntCharges > 0)
        {
            state.guaranteedCritical = true;
            state.bonusPierce += Mathf.Max(0, hunt != null ? hunt.damage : 0);
            huntCharges--;
        }
        var cycle = Get(AdvancedEffect.AmmoCycle);
        if (ammoBurstReady && cycle != null && state.SetBurst(cycle, cycle.value))
        {
            ammoBurstReady = false;
        }
        return state;
    }

    public void OnProjectileHit(Enemy enemy, ProjectileAugmentState state, ref int damage)
    {
        if (!CanAct || enemy == null) return;
        var execution = Get(AdvancedEffect.ExecutionMark);
        if (execution != null && enemy.CurrentHealth <= enemy.MaxHealth * Mathf.Clamp01(execution.threshold))
            damage = Mathf.RoundToInt(damage * Mathf.Max(1f, execution.value));

        var cycle = Get(AdvancedEffect.AmmoCycle);
        if (cycle != null)
        {
            if (ammoBurstReady && state.SetBurst(cycle, cycle.value))
                ammoBurstReady = false;
            if (++ammoHits >= Mathf.Max(1, Mathf.RoundToInt(cycle.threshold)))
            {
                ammoHits = 0;
                ammoBurstReady = true;
            }
        }

    }

    public void OnProjectileResolved(Enemy enemy, ProjectileAugmentState state, int damage)
    {
        if (!CanAct || enemy == null) return;
        if (state.stunDuration > 0f) enemy.Stun(state.stunDuration);
        if (!state.BurstTriggered && state.burstRatio > 0f && state.burstTalent != null)
        {
            state.BurstTriggered = true;
            Pulse(state.burstTalent, enemy.transform.position,
                Mathf.RoundToInt(damage * state.burstRatio), enemy, state.burstTalent.duration);
        }
    }

    public int GetBonusPierce(ProjectileAugmentState state) => Mathf.Max(0, state?.bonusPierce ?? 0);

    private void Pulse(AdvancedTalent config, Vector2 origin, int damage, Enemy excluded, float stun)
    {
        if (config == null) return;
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
        Object.Destroy(effect, 0.25f);
    }
}

public sealed class ProjectileAugmentState
{
    public float damageMultiplier = 1f;
    public float burstRatio;
    public float stunDuration;
    public int bonusPierce;
    public bool guaranteedCritical;
    public bool BurstTriggered;
    public AdvancedTalent burstTalent;

    public bool SetBurst(AdvancedTalent talent, float ratio)
    {
        if (talent == null || burstTalent != null) return false;
        burstTalent = talent;
        burstRatio = ratio;
        BurstTriggered = false;
        return true;
    }
}
