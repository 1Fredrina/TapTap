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

    // stat
    public string statType;
    public float value;

    // weapon
    public string weaponType;
    public float cooldown = 1f;
    public int damage = 1;
    public float range = 12f;

    // projectile
    public int projectileCount = 1;
    public float projectileSpread = 12f;
    public string projectilePrefab;

    // melee
    public float arcAngle = 90f;
    public string effectPrefab;
    public float effectLifetime = 0.2f;

    // aura
    public string auraPrefab;

    // orbit
    public int orbitCount = 2;
    public float orbitRadius = 5f;
    public float orbitSpeed = 120f;
    public string orbitPrefab;

    // skillmod
    public string modType;
    public float modValue;
}