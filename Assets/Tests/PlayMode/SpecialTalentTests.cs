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

    [TestCase("ProjectileWeapon", "Bullet", "projectilePrefab", 1)]
    [TestCase("ShotgunWeapon", "Bullet", "projectilePrefab", 5)]
    [TestCase("PiercingWeapon", "PiercingBullet", "bulletPrefab", 1)]
    [TestCase("RocketWeapon", "RocketProjectile", "rocketPrefab", 1)]
    [TestCase("FireBombWeapon", "FireBombProjectile", "bombPrefab", 1)]
    public void EchoFormRepeatsWholeVolleyAfterDelayAndHalvesFireRate(
        string weaponType, string projectileType, string prefabField, int volleySize)
    {
        Acquire("special_echo_form");
        Assert.That(Number("WeaponCooldownMultiplier"), Is.EqualTo(2f));

        var prefab = new GameObject("Echo Test Bullet Prefab");
        prefab.AddComponent(GameType(projectileType));
        object weapon = Activator.CreateInstance(GameType(weaponType));
        Set(weapon, prefabField, prefab);
        if (prefabField == "projectilePrefab") Set(weapon, "projectileCount", volleySize);
        Set(weapon, "damage", 10);

        Component target = EnemyAt(Vector2.right);
        Call(weapon, "Fire", player, target);
        Call(weapon, "Tick", 0.1f);

        Component[] Spawned() => Object.FindObjectsByType(GameType(projectileType), FindObjectsSortMode.None)
            .OfType<Component>()
            .Where(bullet => bullet.gameObject != prefab && bullet.gameObject.scene == scene)
            .ToArray();
        Assert.That(Spawned().Length, Is.EqualTo(volleySize));
        object effects = Value(player, "SpecialEffects");
        Call(effects, "Tick", 0f);
        Assert.That(Spawned().Length, Is.EqualTo(volleySize), "暂停不应发出回响。");
        Call(effects, "Tick", 0.1f);
        Assert.That(Spawned().Length, Is.EqualTo(volleySize));
        Component[] firstVolley = Spawned();
        player.transform.position = new Vector3(3f, 2f, 0f);
        Call(effects, "Tick", 0.06f);
        Component[] spawned = Spawned();
        Assert.That(spawned.Length, Is.EqualTo(volleySize * 2));
        Assert.That(spawned.Except(firstVolley).All(bullet =>
            Vector3.Distance(bullet.transform.position, player.transform.position) < 0.6f), Is.True,
            "回响应从角色移动后的位置发射。");
        Call(effects, "Tick", 1f);
        Assert.That(Spawned().Length, Is.EqualTo(volleySize * 2), "回响不能继续复制自身。");
        Assert.That(spawned.All(bullet =>
            !float.IsNaN(bullet.transform.position.y) && !float.IsInfinity(bullet.transform.position.y)), Is.True);
        Assert.That(Number("DamageMultiplier"), Is.EqualTo(1f));
    }

    [Test]
    public void EchoQueueKeepsOverlappingVolleysAndDoesNotRepeatWithoutTalent()
    {
        object effects = Value(player, "SpecialEffects");
        int shots = 0;
        Action fire = () => shots++;
        Call(effects, "FireProjectileVolley", fire);
        Call(effects, "Tick", 1f);
        Assert.That(shots, Is.EqualTo(1));

        Acquire("special_echo_form");
        Call(effects, "FireProjectileVolley", fire);
        Call(effects, "Tick", 0.1f);
        Call(effects, "FireProjectileVolley", fire);
        Assert.That(shots, Is.EqualTo(3));
        Call(effects, "Tick", 0.06f);
        Assert.That(shots, Is.EqualTo(4));
        Call(effects, "Tick", 0.1f);
        Assert.That(shots, Is.EqualTo(5));
        Call(effects, "Tick", 1f);
        Assert.That(shots, Is.EqualTo(5));
    }

    [TestCase("disable")]
    [TestCase("death")]
    [TestCase("initialize")]
    public void EchoQueueCancelsWhenOwnerStopsPlaying(string reason)
    {
        Acquire("special_echo_form");
        object effects = Value(player, "SpecialEffects");
        int shots = 0;
        Call(effects, "FireProjectileVolley", (Action)(() => shots++));
        if (reason == "disable")
        {
            player.gameObject.SetActive(false);
            player.gameObject.SetActive(true);
        }
        else if (reason == "death") Call(player, "TakeDamage", 1000);
        else Call(player, "Initialize", data);
        Call(effects, "Tick", 1f);
        Assert.That(shots, Is.EqualTo(1));
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
        Call(player, "AddShield", 100f);
        Assert.That(Number("DamageMultiplier"), Is.EqualTo(1.02f).Within(0.00001f));
        EndInvincibility();
        Call(player, "TakeDamage", 50);
        Assert.That(Number("DamageMultiplier"), Is.EqualTo(1.01f).Within(0.00001f));
        EndInvincibility();
        Call(player, "TakeDamage", 25);
        Assert.That(Number("DamageMultiplier"), Is.EqualTo(1.005f).Within(0.00001f));
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
