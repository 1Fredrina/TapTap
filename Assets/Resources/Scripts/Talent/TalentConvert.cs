using System;
using System.Collections.Generic;
using UnityEngine;

public static class TalentConverter
{
    public static List<TalentBase> Convert(List<TalentJsonEntry> entries)
    {
        var result = new List<TalentBase>();
        if (entries == null) return result;

        foreach (var entry in entries)
        {
            var talent = CreateTalent(entry);
            if (talent != null)
                result.Add(talent);
        }
        return result;
    }

    private static TalentBase CreateTalent(TalentJsonEntry entry)
    {
        if (entry == null) return null;

        TalentColor color = ParseEnum(entry.color, TalentColor.Red);

        switch (entry.type)
        {
            case "stat":
                return new StatTalent
                {
                    id = entry.id,
                    displayName = entry.displayName,
                    description = entry.description,
                    color = color,
                    maxStack = entry.maxStack,
                    weight = entry.weight,
                    prerequisiteIds = entry.prerequisiteIds,
                    statType = ParseEnum(entry.statType, StatType.AttackDamage),
                    value = entry.value
                };

            case "weapon":
                return new WeaponTalent
                {
                    id = entry.id,
                    displayName = entry.displayName,
                    description = entry.description,
                    color = color,
                    maxStack = entry.maxStack,
                    weight = entry.weight,
                    prerequisiteIds = entry.prerequisiteIds,
                    weaponType = ParseEnum(entry.weaponType, WeaponType.Projectile),
                    stackUpMode = ParseEnum(entry.stackUpMode, StackUpMode.ExtraShot),
                    cooldown = entry.cooldown,
                    damage = entry.damage,
                    range = entry.range,
                    projectileCount = entry.projectileCount,
                    projectileSpread = entry.projectileSpread,
                    projectilePrefabPath = entry.projectilePrefab,
                    arcAngle = entry.arcAngle,
                    effectPrefabPath = entry.effectPrefab,
                    effectLifetime = entry.effectLifetime,
                    auraPrefabPath = entry.auraPrefab,
                    orbitCount = entry.orbitCount,
                    orbitRadius = entry.orbitRadius,
                    orbitSpeed = entry.orbitSpeed,
                    orbitPrefabPath = string.IsNullOrEmpty(entry.orbitPrefab)
                        ? "Prefabs/Weapons/OrbitProjectile"
                        : entry.orbitPrefab,
                    rocketPrefabPath = entry.rocketPrefab,
                    explosionEffectPath = entry.explosionEffect,
                    explosionRadius = entry.explosionRadius,
                    rocketSpeed = entry.rocketSpeed,
                    fireBombPrefabPath = entry.fireBombPrefab,
                    firePitPrefabPath = entry.firePitPrefab,
                    firePitRadius = entry.firePitRadius,
                    firePitLifetime = entry.firePitLifetime,
                    firePitDamageInterval = entry.firePitDamageInterval,
                    fireBombSpeed = entry.fireBombSpeed,
                    piercingBulletPrefabPath = entry.piercingBulletPrefab,
                    basePierce = entry.basePierce,
                    piercingBulletSpeed = entry.piercingBulletSpeed
                };

            case "skillmod":
                return new SkillModTalent
                {
                    id = entry.id,
                    displayName = entry.displayName,
                    description = entry.description,
                    color = color,
                    maxStack = entry.maxStack,
                    weight = entry.weight,
                    prerequisiteIds = entry.prerequisiteIds,
                    targetSkillId = entry.targetSkillId,
                    modType = ParseEnum(entry.modType, SkillModType.Duration),
                    value = entry.modValue
                };

            case "killstack":
                return new KillStackTalent
                {
                    id = entry.id,
                    displayName = entry.displayName,
                    description = entry.description,
                    color = color,
                    maxStack = entry.maxStack,
                    weight = entry.weight,
                    prerequisiteIds = entry.prerequisiteIds,
                    initialDamageMultiplier = entry.initialDamageMultiplier,
                    damagePerKill = entry.damagePerKill
                };
            case "stationary":
                return new StationaryTalent
                {
                    id = entry.id,
                    displayName = entry.displayName,
                    description = entry.description,
                    color = color,
                    maxStack = entry.maxStack,
                    weight = entry.weight,
                    prerequisiteIds = entry.prerequisiteIds,
                    attackSpeedMax = entry.stationaryAttackSpeedMax,
                    sizeMax = entry.stationarySizeMax
                };
            case "dashbuff":
                return new DashBuffTalent
                {
                    id = entry.id,
                    displayName = entry.displayName,
                    description = entry.description,
                    color = color,
                    maxStack = entry.maxStack,
                    weight = entry.weight,
                    prerequisiteIds = entry.prerequisiteIds,
                    duration = entry.dashBuffDuration,
                    damageBonus = entry.dashBuffDamageBonus
                };
            case "movespeedtodamage":
                return new MoveSpeedToDamageTalent
                {
                    id = entry.id,
                    displayName = entry.displayName,
                    description = entry.description,
                    color = color,
                    maxStack = entry.maxStack,
                    weight = entry.weight,
                    prerequisiteIds = entry.prerequisiteIds,
                    ratio = entry.moveSpeedToDamageRatio
                };
            case "dashpulse":
                return new DashPulseTalent
                {
                    id = entry.id,
                    displayName = entry.displayName,
                    description = entry.description,
                    color = color,
                    maxStack = entry.maxStack,
                    weight = entry.weight,
                    prerequisiteIds = entry.prerequisiteIds,
                    radius = entry.range,
                    damage = entry.damage,
                    cooldown = entry.cooldown,
                    stunDuration = entry.dashPulseStunDuration,
                    effectPrefabPath = entry.effectPrefab
                };
            case "killfrenzy":
                return new KillFrenzyTalent
                {
                    id = entry.id,
                    displayName = entry.displayName,
                    description = entry.description,
                    color = color,
                    maxStack = entry.maxStack,
                    weight = entry.weight,
                    prerequisiteIds = entry.prerequisiteIds,
                    duration = entry.frenzyDuration,
                    maxCharges = entry.frenzyMaxCharges,
                    attackSpeedPerCharge = entry.frenzyAttackSpeedPerCharge,
                    moveSpeedPerCharge = entry.frenzyMoveSpeedPerCharge
                };
            case "overheal":
                return new OverhealTalent
                {
                    id = entry.id,
                    displayName = entry.displayName,
                    description = entry.description,
                    color = color,
                    maxStack = entry.maxStack,
                    weight = entry.weight,
                    prerequisiteIds = entry.prerequisiteIds,
                    shieldRatio = entry.overhealShieldRatio,
                    shieldCapPercent = entry.overhealShieldCapPercent
                };
            case "retaliation":
                return new RetaliationTalent
                {
                    id = entry.id,
                    displayName = entry.displayName,
                    description = entry.description,
                    color = color,
                    maxStack = entry.maxStack,
                    weight = entry.weight,
                    prerequisiteIds = entry.prerequisiteIds,
                    projectileCount = entry.projectileCount,
                    damage = entry.damage,
                    cooldown = entry.cooldown,
                    projectilePrefabPath = entry.projectilePrefab
                };
            case "travelcharge":
                return new TravelChargeTalent
                {
                    id = entry.id,
                    displayName = entry.displayName,
                    description = entry.description,
                    color = color,
                    maxStack = entry.maxStack,
                    weight = entry.weight,
                    prerequisiteIds = entry.prerequisiteIds,
                    distance = entry.travelDistance,
                    range = entry.range,
                    projectileCount = entry.projectileCount,
                    spread = entry.projectileSpread,
                    damage = entry.damage,
                    projectilePrefabPath = entry.projectilePrefab
                };
            case "shieldbreak":
                return new ShieldBreakTalent
                {
                    id = entry.id,
                    displayName = entry.displayName,
                    description = entry.description,
                    color = color,
                    maxStack = entry.maxStack,
                    weight = entry.weight,
                    prerequisiteIds = entry.prerequisiteIds,
                    radius = entry.range,
                    damage = entry.damage,
                    stunDuration = entry.shieldBreakStunDuration,
                    cooldown = entry.cooldown,
                    effectPrefabPath = entry.effectPrefab
                };
            case "deathnova":
                return new DeathNovaTalent
                {
                    id = entry.id,
                    displayName = entry.displayName,
                    description = entry.description,
                    color = color,
                    maxStack = entry.maxStack,
                    weight = entry.weight,
                    prerequisiteIds = entry.prerequisiteIds,
                    killsRequired = entry.novaKillsRequired,
                    radius = entry.range,
                    damage = entry.damage,
                    effectPrefabPath = entry.effectPrefab
                };
            case "laststand":
                return new LastStandTalent
                {
                    id = entry.id,
                    displayName = entry.displayName,
                    description = entry.description,
                    color = color,
                    maxStack = entry.maxStack,
                    weight = entry.weight,
                    prerequisiteIds = entry.prerequisiteIds,
                    cooldown = entry.cooldown,
                    shieldPercent = entry.lastStandShieldPercent,
                    invincibility = entry.lastStandInvincibility,
                    effectPrefabPath = entry.effectPrefab
                };
            case "compound":
                return new CompoundTalent
                {
                    id = entry.id,
                    displayName = entry.displayName,
                    description = entry.description,
                    color = color,
                    maxStack = entry.maxStack,
                    weight = entry.weight,
                    prerequisiteIds = entry.prerequisiteIds,
                    statModifiers = Array.ConvertAll(entry.statModifiers ?? Array.Empty<StatModifierJsonEntry>(),
                        modifier => new StatModifier
                        {
                            statType = ParseEnum(modifier.statType, StatType.AttackDamage),
                            value = modifier.value
                        })
                };
            default:
                Debug.LogWarning($"未知天赋类型: {entry.type}");
                return null;
        }
    }

    private static T ParseEnum<T>(string value, T fallback) where T : struct
    {
        if (string.IsNullOrEmpty(value)) return fallback;
        return Enum.TryParse(value, true, out T result) ? result : fallback;
    }
}
