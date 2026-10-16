using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public partial class ReactiveTalentTests
{
    [Test]
    public void SpecialTalentsLoadOnceAndMutuallyExclusiveLifeConversionsCannotMix()
    {
        string[] ids =
        {
            "special_cursed_blade", "special_echo_form", "special_pentagram",
            "special_health_to_shield", "special_shield_to_health",
            "special_vitality_power", "special_shield_power"
        };

        foreach (string id in ids)
        {
            Assert.That(talents[id].GetType().Name, Is.EqualTo("SpecialTalent"), id);
            Assert.That((int)talents[id].GetType().GetField("maxStack").GetValue(talents[id]), Is.EqualTo(1), id);
            Assert.That(Call(player, "TryAddTalent", talents[id]), Is.True, id);
            Assert.That(Call(player, "TryAddTalent", talents[id]), Is.False, id);
            Call(player, "Initialize", data);
        }

        Acquire("special_health_to_shield");
        Assert.That(Call(player, "TryAddTalent", talents["special_shield_to_health"]), Is.False);
        Call(player, "Initialize", data);
        Acquire("special_shield_to_health");
        Assert.That(Call(player, "TryAddTalent", talents["special_cursed_blade"]), Is.False);
    }

    [Test]
    public void CursedBladeLocksHealthToOneAndTriplesFinalDamage()
    {
        Acquire("special_cursed_blade");
        Assert.That(Number("MaxHealth"), Is.EqualTo(1f));
        Assert.That(Number("CurrentHealth"), Is.EqualTo(1f));
        Assert.That(Number("DamageMultiplier"), Is.EqualTo(3f));

        Call(player, "ApplyStatTalent", System.Enum.Parse(GameType("StatType"), "MaxHealth"), 1f, 1);
        Assert.That(Number("MaxHealth"), Is.EqualTo(1f));
        Assert.That(Number("DamageMultiplier"), Is.EqualTo(3f));
    }

[Test]
    public void EchoFormDoublesProjectileWeaponShotsAndHalvesFireRate()
    {
        Acquire("special_echo_form");
        Assert.That(Number("WeaponCooldownMultiplier"), Is.EqualTo(2f));

        var prefab = new GameObject("Echo Test Bullet Prefab");
        prefab.AddComponent(GameType("Bullet"));
        object weapon = Activator.CreateInstance(GameType("ProjectileWeapon"));
        Set(weapon, "projectilePrefab", prefab);
        Set(weapon, "projectileCount", 1);
        Set(weapon, "damage", 10);

        Component target = EnemyAt(Vector2.right);
        Call(weapon, "Fire", player, target);
        Call(weapon, "Tick", 0.1f);

        Component[] spawned = Object.FindObjectsByType(GameType("Bullet"), FindObjectsSortMode.None)
            .OfType<Component>()
            .Where(bullet => bullet.gameObject != prefab)
            .ToArray();
        Assert.That(spawned.Length, Is.EqualTo(2));
        Assert.That(spawned.All(bullet =>
            !float.IsNaN(bullet.transform.position.y) && !float.IsInfinity(bullet.transform.position.y)), Is.True);
        Assert.That(Number("DamageMultiplier"), Is.EqualTo(1f));
    }

    [Test]
    public void HealthToShieldConvertsHealthCapAndStopsNaturalShieldDecay()
    {
        Acquire("special_health_to_shield");
        Assert.That(Number("MaxHealth"), Is.EqualTo(1f));
        Assert.That(Number("CurrentHealth"), Is.EqualTo(1f));
        Assert.That(Number("CurrentShield"), Is.EqualTo(198f));

        Call(player, "AddShield", 10f);
        player.GetType().GetMethod("UpdateShield", Private).Invoke(player, null);
        Assert.That(Number("CurrentShield"), Is.EqualTo(208f));

        Call(player, "ApplyStatTalent", System.Enum.Parse(GameType("StatType"), "MaxHealth"), 0.5f, 1);
        Assert.That(Number("CurrentShield"), Is.EqualTo(308f));
        Assert.That(Number("MaxHealth"), Is.EqualTo(1f));
    }

    [Test]
    public void ShieldToHealthConvertsHalfOfEveryShieldGrantIntoMaxHealth()
    {
        Acquire("special_shield_to_health");
        Call(player, "AddShield", 20f);
        Assert.That(Number("CurrentShield"), Is.Zero);
        Assert.That(Number("MaxHealth"), Is.EqualTo(110f));

        Call(player, "AddShield", 10f);
        Assert.That(Number("CurrentShield"), Is.Zero);
        Assert.That(Number("MaxHealth"), Is.EqualTo(115f));
    }

    [Test]
    public void HealthAndShieldDamageBonusesTrackTheirCurrentResource()
    {
        Acquire("special_vitality_power");
        Assert.That(Number("DamageMultiplier"), Is.EqualTo(2f));
        Call(player, "TakeDamage", 50);
        Assert.That(Number("DamageMultiplier"), Is.EqualTo(1.5f));

        Call(player, "Initialize", data);
        Acquire("special_shield_power");
        Call(player, "AddShield", 20f);
        Assert.That(Number("DamageMultiplier"), Is.EqualTo(1.2f));
        EndInvincibility();
        Call(player, "TakeDamage", 10);
        Assert.That(Number("DamageMultiplier"), Is.EqualTo(1.1f));
    }

    [UnityTest]
    public IEnumerator PentagramEliminatesVisibleEnemiesWithoutExperience()
    {
        var cameraObject = new GameObject("Pentagram Test Camera");
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;

        Component visibleEnemy = EnemyAt(Vector2.right);
        Component outsideEnemy = EnemyAt(new Vector2(100f, 0f));
        Acquire("special_pentagram");
        Call(Value(player, "SpecialEffects"), "Tick", 90f);

        Assert.That(Value(visibleEnemy, "IsDead"), Is.True);
        Assert.That(Value(outsideEnemy, "IsDead"), Is.False);
        Assert.That(Value(player, "Experience"), Is.Zero);
        Assert.That(GameObject.Find("Pentagram Screen Effect"), Is.Not.Null);

        Call(Value(player, "SpecialEffects"), "Tick", 1f);
        yield return null;
        Assert.That(GameObject.Find("Pentagram Screen Effect"), Is.Null);
    }

    [Test]
    public void ShieldEnemyFacesPlayerBlocksFrontHitsAndRegeneratesAfterBreaking()
    {
        ((Component)player).transform.position = new Vector3(3f, 4f, 0f);
        var enemyObject = new GameObject("Shield Enemy Test");
        enemyObject.AddComponent<Rigidbody2D>();
        Component shieldEnemy = enemyObject.AddComponent(GameType("ShieldEnemy"));
        object enemyData = Activator.CreateInstance(GameType("EnemyData"));
        Set(enemyData, "MaxHealth", 100f);
        Set(enemyData, "MoveSpeed", 0f);
        shieldEnemy.GetType().GetMethod("Initialize", new[] { enemyData.GetType() })
            .Invoke(shieldEnemy, new[] { enemyData });

        shieldEnemy.GetType().GetMethod("UpdateShieldFacing", Private).Invoke(shieldEnemy, null);
        Vector2 facing = (Vector2)Value(shieldEnemy, "ShieldDirection");
        Assert.That(Vector2.Dot(facing, new Vector2(0.6f, 0.8f)), Is.GreaterThan(0.99f));
        Assert.That(shieldEnemy.GetComponentInChildren<SpriteRenderer>().enabled, Is.True);

        Call(shieldEnemy, "TakeDamageFrom", 40, new Vector2(3f, 4f));
        Assert.That((float)Value(shieldEnemy, "CurrentShieldHealth"), Is.EqualTo(20f));
        Assert.That((float)Value(shieldEnemy, "CurrentHealth"), Is.EqualTo(100f));

        Call(shieldEnemy, "TakeDamageFrom", 20, new Vector2(-3f, -4f));
        Assert.That((float)Value(shieldEnemy, "CurrentShieldHealth"), Is.EqualTo(20f));
        Assert.That((float)Value(shieldEnemy, "CurrentHealth"), Is.EqualTo(80f));

        Call(shieldEnemy, "TakeDamageFrom", 40, new Vector2(3f, 4f));
        Assert.That((float)Value(shieldEnemy, "CurrentShieldHealth"), Is.Zero);
        Assert.That((float)Value(shieldEnemy, "CurrentHealth"), Is.EqualTo(60f));
        Assert.That(shieldEnemy.GetComponentInChildren<SpriteRenderer>().enabled, Is.False);

        shieldEnemy.GetType().GetMethod("UpdateShieldRecharge", Private).Invoke(shieldEnemy, new object[] { 3.5f });
        Assert.That((float)Value(shieldEnemy, "CurrentShieldHealth"), Is.EqualTo(6f).Within(0.001f));
    }
}
