using System;

[Serializable]
public class TalentJsonEntry
{
    public string type;
    public string id;
    public string displayName;
    public string description;
    public string color;
    public int maxStack = 1;
    public int weight = 10;

    // 前置条件
    public string[] prerequisiteIds;

    // stat
    public string statType;
    public float value;

    // weapon
    public string weaponType;
    public float cooldown = 1f;
    public int damage = 1;
    public float range = 12f;
    public string stackUpMode;

    public int projectileCount = 1;
    public float projectileSpread = 12f;
    public string projectilePrefab;

    public float arcAngle = 90f;
    public string effectPrefab;
    public float effectLifetime = 0.2f;

    public string auraPrefab;

    public int orbitCount = 2;
    public float orbitRadius = 5f;
    public float orbitSpeed = 120f;
    public string orbitPrefab;

    public string rocketPrefab;
    public string explosionEffect;
    public float explosionRadius = 2f;
    public float rocketSpeed = 8f;

    public string fireBombPrefab;
    public string firePitPrefab;
    public float firePitRadius = 2f;
    public float firePitLifetime = 3f;
    public float firePitDamageInterval = 0.5f;
    public float fireBombSpeed = 8f;

    public string piercingBulletPrefab;
    public int basePierce = 1;
    public float piercingBulletSpeed = 12f;

    // skillmod
    public string targetSkillId;
    public string modType;
    public float modValue;

    // killstack
    public float initialDamageMultiplier = 1f;
    public float damagePerKill = 0f;

    public float stationaryAttackSpeedMax = 0f;
    public float stationarySizeMax = 0f;
}