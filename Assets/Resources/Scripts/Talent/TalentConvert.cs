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
                    weaponType = ParseEnum(entry.weaponType, WeaponType.Projectile),
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
                        : entry.orbitPrefab
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
                    modType = ParseEnum(entry.modType, SkillModType.Duration),
                    value = entry.modValue
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