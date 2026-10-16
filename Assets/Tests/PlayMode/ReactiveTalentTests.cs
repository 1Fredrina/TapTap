using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public partial class ReactiveTalentTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private Scene scene, previousScene, otherScene;
    private Component player;
    private object data;
    private Dictionary<string, object> talents;
    private FieldInfo activePlayerField, managerField;
    private object previousPlayer, previousManager;

    private static Type GameType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Call(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method).Invoke(target, args);
    private static object Value(object target, string property) => target.GetType().GetProperty(property).GetValue(target);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field).SetValue(target, value);
    private void Tick(float seconds) => player.GetType().GetMethod("TickReactiveTalents", Private).Invoke(player, new object[] { seconds });
    private float Number(string property) => (float)Value(player, property);

    [SetUp]
    public void SetUp()
    {
        previousScene = SceneManager.GetActiveScene();
        scene = SceneManager.CreateScene("Reactive Talents " + Guid.NewGuid(),
            new CreateSceneParameters(LocalPhysicsMode.Physics2D));
        SceneManager.SetActiveScene(scene);
        activePlayerField = GameType("Player").GetField("activePlayer", BindingFlags.Static | BindingFlags.NonPublic);
        managerField = GameType("GameManager").GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
        previousPlayer = activePlayerField.GetValue(null);
        previousManager = managerField.GetValue(null);
        activePlayerField.SetValue(null, null);
        managerField.SetValue(null, null);
        player = new GameObject("Talent Test Player").AddComponent(GameType("Player"));
        data = Activator.CreateInstance(GameType("PlayerData"));
        Set(data, "MaxHealth", 100f);
        Set(data, "BaseExperienceToNextLevel", 100000);
        Call(player, "Initialize", data);

        // 读取实际 JSON 并走正式转换器，避免只验证手工构造的天赋。
        Type entriesType = typeof(List<>).MakeGenericType(GameType("TalentJsonEntry"));
        MethodInfo read = GameType("LitJson.JsonMapper").GetMethods().Single(method =>
            method.Name == "ToObject" && method.IsGenericMethodDefinition && method.GetParameters().Length == 1 &&
            method.GetParameters()[0].ParameterType == typeof(string));
        object entries = read.MakeGenericMethod(entriesType).Invoke(null,
            new object[] { File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "TalentData.json")) });
        var converted = (IEnumerable)GameType("TalentConverter").GetMethod("Convert").Invoke(null, new[] { entries });
        talents = converted.Cast<object>().ToDictionary(t => (string)t.GetType().GetField("id").GetValue(t));
        Assert.That(talents.Count, Is.EqualTo(((ICollection)entries).Count), "每条配置都必须成功转换，包含 compound。");
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject root in scene.GetRootGameObjects()) Object.DestroyImmediate(root);
        if (otherScene.IsValid())
            foreach (GameObject root in otherScene.GetRootGameObjects()) Object.DestroyImmediate(root);
        activePlayerField.SetValue(null, previousPlayer);
        managerField.SetValue(null, previousManager);
        SceneManager.SetActiveScene(previousScene);
    }

    [UnityTearDown]
    public IEnumerator UnloadScenes()
    {
        if (otherScene.IsValid()) yield return SceneManager.UnloadSceneAsync(otherScene);
        if (scene.IsValid()) yield return SceneManager.UnloadSceneAsync(scene);
    }

    private void Acquire(string id, int ranks = 1)
    {
        for (int i = 0; i < ranks; i++)
            Assert.That(Call(player, "TryAddTalent", talents[id]), Is.True, id);
    }

    private void Kill() => GameType("Player").GetMethod("NotifyEnemyDefeated", new[] { typeof(int) }).Invoke(null, new object[] { 0 });
    private void EndInvincibility() => player.GetType().GetField("invincibleTimer", Private).SetValue(player, 0f);
    private Component[] Bullets() => Object.FindObjectsByType(GameType("PiercingBullet"), FindObjectsSortMode.None)
        .Cast<Component>().Where(b => b.gameObject.scene == scene).ToArray();

    private Component EnemyAt(Vector2 position, int layer = 7)
    {
        var enemy = new GameObject("Talent Test Enemy");
        enemy.layer = layer;
        enemy.transform.position = position;
        Component component = enemy.AddComponent(GameType("Enemy"));
        object enemyData = Activator.CreateInstance(GameType("EnemyData"));
        Set(enemyData, "MaxHealth", 100f);
        Set(enemyData, "MoveSpeed", 0f);
        component.GetType().GetMethod("Initialize", new[] { enemyData.GetType() })
            .Invoke(component, new[] { enemyData });
        return component;
    }

    [TestCase("dash_pulse", "DashPulseTalent")]
    [TestCase("kill_frenzy", "KillFrenzyTalent")]
    [TestCase("overheal_barrier", "OverhealTalent")]
    [TestCase("retaliation_burst", "RetaliationTalent")]
    [TestCase("travel_capacitor", "TravelChargeTalent")]
    [TestCase("shell_rupture", "ShieldBreakTalent")]
    [TestCase("death_nova", "DeathNovaTalent")]
    public void ConfiguredTalentsAcquireAndRespectRankCap(string id, string type)
    {
        Assert.That(talents[id].GetType().Name, Is.EqualTo(type));
        Acquire(id, 3);
        Assert.That(Call(player, "TryAddTalent", talents[id]), Is.False);
    }

    private object Effects => Value(player, "TalentEffects");

    [TestCase("cell_repair", "HealthRegeneration", 1f)]
    [TestCase("healing_affinity", "HealingReceived", 0.2f)]
    [TestCase("membrane_dodge", "DodgeChance", 0.08f)]
    [TestCase("neural_resilience", "StunResistance", 0.2f)]
    [TestCase("dash_extension", "DashDistance", 0.25f)]
    [TestCase("antibody_critical", "ProjectileCriticalChance", 0.1f)]
    [TestCase("antibody_persistence", "ProjectileLifetime", 0.25f)]
    [TestCase("antibody_size", "ProjectileSize", 0.2f)]
    [TestCase("antibody_homing", "ProjectileHoming", 60f)]
    [TestCase("antibody_ricochet", "ProjectileRicochet", 1f)]
    public void FoundationTalentsStackUnlockPrerequisitesAndReset(string id, string stat, float amount)
    {
        object derived = Activator.CreateInstance(GameType("StatTalent"));
        Set(derived, "prerequisiteIds", new[] { id });
        Assert.That(Call(player, "HasPrerequisites", derived), Is.False);
        Assert.That(Call(player, "HasPrerequisites", talents[id]), Is.True);
        Acquire(id, 3);
        object statType = Enum.Parse(GameType("StatType"), stat);
        Assert.That((float)Call(player, "GetStatBonus", statType), Is.EqualTo(amount * 3).Within(0.001f));
        Assert.That(Call(player, "HasPrerequisites", derived), Is.True);
        Assert.That(Call(player, "TryAddTalent", talents[id]), Is.False);
        Call(player, "Initialize", data);
        Assert.That((float)Call(player, "GetStatBonus", statType), Is.Zero);
        Assert.That(Call(player, "HasPrerequisites", derived), Is.False);
    }

    private void Regen(float seconds) => player.GetType().GetMethod("TickHealthRegeneration", Private)
        .Invoke(player, new object[] { seconds });

    [Test]
    public void RegenerationAndHealingBonusRespectPauseDeathOverflowAndReset()
    {
        Acquire("cell_repair", 2);
        Acquire("healing_affinity");
        Call(player, "TakeDamage", 50);
        Regen(0f);
        Assert.That(Number("CurrentHealth"), Is.EqualTo(50f));
        Regen(5f);
        Assert.That(Number("CurrentHealth"), Is.EqualTo(62f).Within(0.001f));
        Call(player, "Heal", 10f);
        Assert.That(Number("CurrentHealth"), Is.EqualTo(74f).Within(0.001f));
        Call(player, "AddShield", 10f);
        Assert.That(Number("CurrentShield"), Is.EqualTo(10f));
        Acquire("overheal_barrier");
        Call(player, "Heal", 30f); // 36 治疗中溢出 10，转化 5 护盾。
        Assert.That(Number("CurrentHealth"), Is.EqualTo(100f));
        Assert.That(Number("CurrentShield"), Is.EqualTo(15f).Within(0.001f));
        player.gameObject.SetActive(false);
        Regen(10f);
        Assert.That(Number("CurrentShield"), Is.EqualTo(15f).Within(0.001f));
        player.gameObject.SetActive(true);
        EndInvincibility();
        Call(player, "TakeDamage", 1000);
        Regen(10f);
        Assert.That(Number("CurrentHealth"), Is.Zero);
        Call(player, "Initialize", data);
        EndInvincibility();
        Call(player, "TakeDamage", 20);
        Regen(10f);
        Assert.That(Number("CurrentHealth"), Is.EqualTo(80f));
    }

    [Test]
    public void DodgePreservesShieldAndDoesNotTriggerRetaliation()
    {
        Acquire("retaliation_burst");
        Call(player, "ApplyStatTalent", Enum.Parse(GameType("StatType"), "DodgeChance"), 1f, 1);
        Call(player, "AddShield", 10f);
        Call(player, "TakeDamage", 1000);
        Assert.That(Number("CurrentHealth"), Is.EqualTo(100f));
        Assert.That(Number("CurrentShield"), Is.EqualTo(10f));
        Assert.That(Value(player, "IsInvincible"), Is.True);
        Assert.That(Bullets(), Is.Empty);
        Call(player, "Initialize", data);
        EndInvincibility();
        Call(player, "TakeDamage", 10);
        Assert.That(Number("CurrentHealth"), Is.EqualTo(90f));
    }

    [Test]
    public void DashExtensionAndStunResistanceReachTheController()
    {
        Component controller = player.gameObject.AddComponent(GameType("PlayerController"));
        Call(player, "Initialize", data);
        Acquire("neural_resilience", 3);
        Acquire("dash_extension", 2);
        Call(controller, "ApplyStun", 2f);
        Assert.That((float)controller.GetType().GetField("stunTimer", Private).GetValue(controller), Is.EqualTo(0.8f).Within(0.001f));
        controller.GetType().GetField("stunTimer", Private).SetValue(controller, 0f);
        controller.GetType().GetField("dashRequested", Private).SetValue(controller, true);
        controller.GetType().GetMethod("FixedUpdate", Private).Invoke(controller, null);
        Assert.That((float)controller.GetType().GetField("dashTime", Private).GetValue(controller),
            Is.EqualTo(0.15f * 1.5f - Time.fixedDeltaTime).Within(0.001f));
        Assert.That((float)controller.GetType().GetField("dashCooldownTimer", Private).GetValue(controller), Is.EqualTo(1f));
    }

    private Component FoundationBullet(string type, int pierce = 0)
    {
        Component bullet = new GameObject("Foundation Bullet").AddComponent(GameType(type));
        Call(bullet, "SetTalentOwner", player);
        if (type == "Bullet")
        {
            bullet.GetType().GetField("hitLayers", Private).SetValue(bullet, (LayerMask)(~0));
            bullet.GetType().GetMethod("Init", new[] { typeof(Vector2), typeof(int), typeof(int), typeof(float) })
                .Invoke(bullet, new object[] { Vector2.right, 10, pierce, 1f });
        }
        else Call(bullet, "Init", Vector2.right, 10f, pierce, 1f);
        return bullet;
    }

    private void Hit(Component bullet, Component enemy)
    {
        Collider2D collider = enemy.GetComponent<Collider2D>();
        if (collider == null) collider = enemy.gameObject.AddComponent<CircleCollider2D>();
        bullet.GetType().GetMethod("OnTriggerEnter2D", Private).Invoke(bullet, new object[] { collider });
    }

    [TestCase("Bullet")]
    [TestCase("PiercingBullet")]
    public void ProjectileStatsSnapshotOwnerAndHomingTurnsWithoutHittingOtherScenes(string type)
    {
        Acquire("antibody_persistence", 2);
        Acquire("antibody_size");
        Acquire("antibody_homing");
        Component bullet = FoundationBullet(type);
        Assert.That(bullet.transform.localScale.x, Is.EqualTo(1.2f).Within(0.001f));
        Assert.That((float)bullet.GetType().GetField("lifeTimer", Private).GetValue(bullet), Is.EqualTo(4.5f).Within(0.001f));
        Component target = EnemyAt(new Vector2(0f, 3f));
        Component ignored = EnemyAt(new Vector2(0f, -1f));
        otherScene = SceneManager.CreateScene("Foundation Other " + Guid.NewGuid());
        SceneManager.MoveGameObjectToScene(ignored.gameObject, otherScene);
        object effects = bullet.GetType().GetField("talentEffects", Private).GetValue(bullet);
        Vector2 paused = (Vector2)Call(effects, "Steer", Vector2.zero, Vector2.right, 0f);
        Assert.That(paused, Is.EqualTo(Vector2.right));
        Vector2 steered = (Vector2)Call(effects, "Steer", Vector2.zero, Vector2.right, 0.5f);
        Assert.That(Vector2.Angle(Vector2.right, steered), Is.EqualTo(30f).Within(0.001f));
        Assert.That(steered.y, Is.GreaterThan(0f));
        Hit(bullet, ignored);
        Assert.That(Value(ignored, "CurrentHealth"), Is.EqualTo(100f));
        Call(player, "Initialize", data);
        Component fresh = FoundationBullet(type);
        Assert.That(fresh.transform.localScale.x, Is.EqualTo(1f));
        Assert.That((float)fresh.GetType().GetField("lifeTimer", Private).GetValue(fresh), Is.EqualTo(3f));
        Assert.That((float)Value(effects, "LifetimeMultiplier"), Is.EqualTo(1.5f));
    }

    [TestCase("Bullet")]
    [TestCase("PiercingBullet")]
    public void RicochetFollowsPierceAndNeverHitsTheSameEnemyTwice(string type)
    {
        Acquire("antibody_ricochet");
        Call(player, "ApplyStatTalent", Enum.Parse(GameType("StatType"), "ProjectileCriticalChance"), 1f, 1);
        Component bullet = FoundationBullet(type, 1);
        Component first = EnemyAt(new Vector2(1f, 0f));
        Component second = EnemyAt(new Vector2(0f, 2f));
        Component third = EnemyAt(new Vector2(0f, 3f));
        Hit(bullet, first);
        Hit(bullet, first);
        Assert.That(Value(first, "CurrentHealth"), Is.EqualTo(80f));
        Assert.That((Vector2)bullet.GetType().GetField("direction", Private).GetValue(bullet), Is.EqualTo(Vector2.right));
        Hit(bullet, second);
        Vector2 direction = (Vector2)bullet.GetType().GetField("direction", Private).GetValue(bullet);
        Assert.That(direction.y, Is.EqualTo(1f).Within(0.001f));
        Assert.That((bool)bullet.GetType().GetField("spent", Private).GetValue(bullet), Is.False);
        Hit(bullet, third);
        Assert.That((bool)bullet.GetType().GetField("spent", Private).GetValue(bullet), Is.True);
        Assert.That(Value(third, "CurrentHealth"), Is.EqualTo(80f));
    }

    [TestCase("Bullet")]
    [TestCase("PiercingBullet")]
    public void RicochetWithoutAnAvailableTargetStillExpires(string type)
    {
        Acquire("antibody_ricochet");
        Component bullet = FoundationBullet(type);
        Component onlyTarget = EnemyAt(Vector2.right);
        EnemyAt(new Vector2(20f, 0f));
        Hit(bullet, onlyTarget);
        Assert.That((bool)bullet.GetType().GetField("spent", Private).GetValue(bullet), Is.True);
        Assert.That(Value(onlyTarget, "CurrentHealth"), Is.EqualTo(90f));
    }

    [Test]
    public void LevelDamageGrowthStacksWithTalentsAndUpdatesExistingOrbit()
    {
        object gameData = GameType("GameDataMgr").GetProperty("Instance").GetValue(null);
        FieldInfo talentPool = gameData.GetType().GetField("talentDatas");
        object previousPool = talentPool.GetValue(gameData);
        // 此用例只验证升级战斗收益，不打开天赋选择 UI。
        talentPool.SetValue(gameData, null);
        try
        {
            Set(data, "BaseExperienceToNextLevel", 5);
            Set(data, "MaxLevel", 3);
            Assert.That(Number("DamageMultiplier"), Is.EqualTo(1f));
            Call(player, "AddExperience", 5);
            Assert.That(Value(player, "Level"), Is.EqualTo(2));
            Assert.That(Number("DamageMultiplier"), Is.EqualTo(1.1f).Within(0.001f));

            Acquire("weapon_orbit");
            Component orbit = Object.FindObjectsByType(GameType("OrbitProjectile"), FindObjectsSortMode.None)
                .Cast<Component>().First(component => component.gameObject.scene == scene);
            Call(player, "AddExperience", 10);
            Assert.That(Value(player, "Level"), Is.EqualTo(3));
            Assert.That(Number("DamageMultiplier"), Is.EqualTo(1.2f).Within(0.001f));
            Acquire("double_edge_damage");
            Assert.That(Number("DamageMultiplier"), Is.EqualTo(1.8f).Within(0.001f));

            Component enemy = EnemyAt(Vector2.one);
            Collider2D collider = enemy.gameObject.AddComponent<CircleCollider2D>();
            orbit.GetType().GetMethod("OnTriggerEnter2D", Private).Invoke(orbit, new object[] { collider });
            Assert.That(Value(enemy, "CurrentHealth"), Is.EqualTo(82f), "旧环绕物命中时也应使用新等级和天赋倍率，且不能重复乘算。");

            Call(player, "AddExperience", 1000);
            Assert.That(Value(player, "Level"), Is.EqualTo(3));
            Assert.That(Number("DamageMultiplier"), Is.EqualTo(1.8f).Within(0.001f));
            Call(player, "Initialize", data);
            Assert.That(Value(player, "Level"), Is.EqualTo(1));
            Assert.That(Number("DamageMultiplier"), Is.EqualTo(1f));
        }
        finally
        {
            talentPool.SetValue(gameData, previousPool);
        }
    }

    private void TickEffects(float seconds) => Call(Effects, "Tick", seconds);
    private void MoveAndTick(Vector2 position, float seconds = 0.2f)
    {
        player.transform.position = position;
        TickEffects(seconds);
    }

    [Test]
    public void TravelChargeWaitsForTargetAndFiresAimedFanWithBonuses()
    {
        Acquire("travel_capacitor", 2);
        Acquire("double_edge_damage");
        Acquire("extra_pierce");
        Acquire("bullet_speed");
        TickEffects(5f);
        Assert.That(Value(Effects, "TravelCharge"), Is.EqualTo(0f), "静止不能蓄能。");
        for (int i = 1; i <= 12; i++) MoveAndTick(new Vector2(i, 0f));
        Assert.That(Value(Effects, "TravelCharge"), Is.EqualTo(1f));
        Assert.That(Bullets(), Is.Empty);
        EnemyAt(new Vector2(12f, 6f));
        TickEffects(0.2f);
        var bullets = Bullets();
        Assert.That(bullets.Length, Is.EqualTo(5));
        Vector2 sum = Vector2.zero;
        var angles = new List<float>();
        foreach (Component bullet in bullets)
        {
            Assert.That(bullet.GetType().GetField("damage").GetValue(bullet), Is.EqualTo(18f));
            Assert.That(bullet.GetType().GetField("pierceRemaining").GetValue(bullet), Is.EqualTo(1));
            Assert.That((float)bullet.GetType().GetField("speed").GetValue(bullet), Is.EqualTo(13.8f).Within(0.001f));
            Assert.That((float)bullet.GetType().GetField("lifeTimer", Private).GetValue(bullet), Is.GreaterThan(0f));
            Vector2 direction = (Vector2)bullet.GetType().GetField("direction", Private).GetValue(bullet);
            sum += direction;
            angles.Add(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }
        angles.Sort();
        Assert.That(angles, Is.EqualTo(new[] { 60f, 75f, 90f, 105f, 120f }).Within(0.001f));
        Assert.That(sum.normalized.y, Is.EqualTo(1f).Within(0.001f), "扇形中央必须瞄准上方目标。");
        Assert.That(Value(Effects, "TravelCharge"), Is.EqualTo(0f));
        TickEffects(1f);
        Assert.That(Bullets().Length, Is.EqualTo(5), "空场最多存一轮，不能囤积无限连发。");
    }

    [Test]
    public void TravelChargeIgnoresPauseLargeRelocationAndDisabledCharacter()
    {
        Acquire("travel_capacitor");
        MoveAndTick(new Vector2(100f, 0f));
        Assert.That(Value(Effects, "TravelCharge"), Is.EqualTo(0f));
        MoveAndTick(new Vector2(101f, 0f), 0f);
        Assert.That(Value(Effects, "TravelCharge"), Is.EqualTo(0f));
        MoveAndTick(new Vector2(103f, 0f));
        Assert.That(Value(Effects, "TravelCharge"), Is.EqualTo(0.25f));
        player.gameObject.SetActive(false);
        MoveAndTick(new Vector2(105f, 0f));
        player.transform.position = new Vector2(106f, 0f);
        player.gameObject.SetActive(true);
        TickEffects(1f);
        Assert.That(Value(Effects, "TravelCharge"), Is.EqualTo(0.25f));
        Acquire("travel_capacitor");
        Assert.That(Value(Effects, "TravelCharge"), Is.EqualTo(0.25f), "升级保留已蓄能量。");
    }

    [Test]
    public void ShieldRuptureOnlyFiresOnDamageBreakAndHonorsCooldownAndTargetFilters()
    {
        Assert.That(Call(player, "HasPrerequisites", talents["shell_rupture"]), Is.False);
        Acquire("shield_on_kill");
        Assert.That(Call(player, "HasPrerequisites", talents["shell_rupture"]), Is.True);
        Acquire("shell_rupture", 2);
        player.GetType().GetField("enemyLayers", Private).SetValue(player, (LayerMask)(1 << 7));
        Component near = EnemyAt(Vector2.one);
        near.gameObject.AddComponent<BoxCollider2D>();
        near.gameObject.AddComponent<CircleCollider2D>();
        Component far = EnemyAt(new Vector2(4f, 0f));
        Component excluded = EnemyAt(Vector2.one, 0);
        Component elsewhere = EnemyAt(Vector2.one);
        otherScene = SceneManager.CreateScene("Rupture Other " + Guid.NewGuid());
        SceneManager.MoveGameObjectToScene(elsewhere.gameObject, otherScene);
        Call(player, "AddShield", 10f);
        Call(player, "TakeDamage", 5);
        Assert.That(Value(near, "CurrentHealth"), Is.EqualTo(100f));
        EndInvincibility();
        Call(player, "TakeDamage", 5); // 恰好破盾，不损失生命也触发。
        Assert.That(Number("CurrentHealth"), Is.EqualTo(100f));
        Assert.That(Value(near, "CurrentHealth"), Is.EqualTo(60f));
        Assert.That(Value(near, "IsStunned"), Is.True);
        foreach (Component enemy in new[] { far, excluded, elsewhere })
            Assert.That(Value(enemy, "CurrentHealth"), Is.EqualTo(100f));
        EndInvincibility();
        Call(player, "AddShield", 10f);
        Call(player, "TakeDamage", 10);
        Assert.That(Value(near, "CurrentHealth"), Is.EqualTo(60f));
        TickEffects(6f);
        EndInvincibility();
        Call(player, "AddShield", 10f);
        Call(player, "TakeDamage", 10);
        Assert.That(Value(near, "CurrentHealth"), Is.EqualTo(20f));
    }

    [Test]
    public void ShieldDecayAndFatalDamageDoNotTriggerRupture()
    {
        Acquire("shell_rupture");
        Component enemy = EnemyAt(Vector2.one);
        Call(player, "AddShield", 1f);
        Set(data, "ShieldDecayRate", 100000000f);
        player.GetType().GetMethod("UpdateShield", Private).Invoke(player, null);
        Assert.That(Number("CurrentShield"), Is.Zero);
        Assert.That(Value(enemy, "CurrentHealth"), Is.EqualTo(100f));
        Call(player, "AddShield", 1f);
        Call(player, "TakeDamage", 1000);
        Assert.That(Value(player, "IsDead"), Is.True);
        Assert.That(Value(enemy, "CurrentHealth"), Is.EqualTo(100f));
    }

    [Test]
    public void DeathNovaExplodesAtVictimAndDoesNotRecursivelyChargeItself()
    {
        Acquire("death_nova");
        Acquire("heal_on_kill");
        Acquire("kill_frenzy");
        Call(player, "TakeDamage", 80);
        for (int i = 0; i < 4; i++) Call(EnemyAt(new Vector2(20f, 0f)), "TakeDamage", 1000);
        Assert.That(Value(Effects, "NovaKills"), Is.EqualTo(4));
        Component atPlayer = EnemyAt(Vector2.one);
        Component survivor = EnemyAt(new Vector2(10f, 1f));
        var victims = Enumerable.Range(0, 6).Select(_ => EnemyAt(new Vector2(11f, 0f))).ToArray();
        foreach (Component victim in victims) Call(victim, "TakeDamage", 90);
        int experienceBefore = (int)Value(player, "Experience");
        Call(EnemyAt(new Vector2(10f, 0f)), "TakeDamage", 1000);
        Assert.That(Value(survivor, "CurrentHealth"), Is.EqualTo(82f));
        Assert.That(Value(atPlayer, "CurrentHealth"), Is.EqualTo(100f), "爆炸原点为死亡位置而非玩家。");
        Assert.That(victims.All(victim => (bool)Value(victim, "IsDead")), Is.True);
        Assert.That(Value(Effects, "NovaKills"), Is.EqualTo(0), "爆炸击杀超过阈值也不能递归产生新星。");
        Assert.That(Number("CurrentHealth"), Is.EqualTo(53f), "所有 11 次击杀仍触发自噬。");
        Assert.That(Value(player, "KillFrenzyCharges"), Is.EqualTo(5));
        Assert.That((int)Value(player, "Experience"), Is.GreaterThan(experienceBefore));
        Call(EnemyAt(new Vector2(20f, 0f)), "TakeDamage", 1000);
        Assert.That(Value(Effects, "NovaKills"), Is.EqualTo(1));
    }

    [Test]
    public void LastStandRescuesOnceAndStillAllowsDeathDuringCooldown()
    {
        Acquire("last_molt");
        Assert.That(Call(player, "TryAddTalent", talents["last_molt"]), Is.False);
        Acquire("retaliation_burst");
        Call(player, "AddShield", 200f);
        Call(player, "TakeDamage", 100);
        Assert.That(Value(Effects, "LastStandCooldownRemaining"), Is.EqualTo(0f));
        EndInvincibility();
        Call(player, "TakeDamage", 1000);
        Assert.That(Value(player, "IsDead"), Is.False);
        Assert.That(Number("CurrentHealth"), Is.EqualTo(1f));
        Assert.That(Number("CurrentShield"), Is.EqualTo(35f).Within(0.001f));
        Assert.That((float)player.GetType().GetField("invincibleTimer", Private).GetValue(player), Is.GreaterThanOrEqualTo(2f));
        Assert.That(Value(Effects, "LastStandCooldownRemaining"), Is.EqualTo(45f));
        Assert.That(Bullets().Length, Is.EqualTo(8), "保命后存活，仍可触发应激散射。");
        Call(player, "TakeDamage", 1000);
        Assert.That(Number("CurrentHealth"), Is.EqualTo(1f));
        EndInvincibility();
        Call(player, "TakeDamage", 1000);
        Assert.That(Value(player, "IsDead"), Is.True);
        Assert.That(Bullets().Length, Is.EqualTo(8));
    }

    [Test]
    public void LastStandRechargesAndPreservesLongerInvincibility()
    {
        Acquire("last_molt");
        Set(data, "InvincibleTime", 5f);
        Call(player, "TakeDamage", 1000);
        Assert.That((float)player.GetType().GetField("invincibleTimer", Private).GetValue(player), Is.EqualTo(5f));
        TickEffects(44f);
        Assert.That(Value(Effects, "LastStandCooldownRemaining"), Is.EqualTo(1f));
        TickEffects(0f);
        Assert.That(Value(Effects, "LastStandCooldownRemaining"), Is.EqualTo(1f));
        TickEffects(1f);
        EndInvincibility();
        Call(player, "TakeDamage", 1000);
        Assert.That(Value(player, "IsDead"), Is.False);
        Assert.That(Value(Effects, "LastStandCooldownRemaining"), Is.EqualTo(45f));
    }

    [Test]
    public void ExtendedEffectsBelongToEachCharacterAndResetOnInitialize()
    {
        Acquire("travel_capacitor");
        Acquire("shell_rupture");
        Acquire("death_nova");
        Acquire("last_molt");
        MoveAndTick(new Vector2(2f, 0f));
        Call(EnemyAt(Vector2.one), "TakeDamage", 1000);
        Component otherPlayer = new GameObject("Other Talent Owner").AddComponent(GameType("Player"));
        object otherEffects = Value(otherPlayer, "TalentEffects");
        Assert.That(Value(otherEffects, "HasLastStand"), Is.False);
        Assert.That(Value(otherEffects, "TravelCharge"), Is.EqualTo(0f));
        Assert.That(Value(otherEffects, "KillsPerNova"), Is.EqualTo(0));
        Call(player, "Initialize", data);
        Assert.That(Value(Effects, "HasLastStand"), Is.False);
        Assert.That(Value(Effects, "TravelCharge"), Is.EqualTo(0f));
        Assert.That(Value(Effects, "KillsPerNova"), Is.EqualTo(0));
        Component enemy = EnemyAt(Vector2.one);
        Call(player, "AddShield", 5f);
        Call(player, "TakeDamage", 5);
        Assert.That(Value(enemy, "CurrentHealth"), Is.EqualTo(100f));
        EndInvincibility();
        Call(player, "TakeDamage", 1000);
        Assert.That(Value(player, "IsDead"), Is.True);
    }

    [Test]
    public void ExistingCompoundTalentsStillApplyBothModifiers()
    {
        Acquire("double_edge_damage");
        Assert.That(Number("DamageMultiplier"), Is.EqualTo(1.5f).Within(0.001f));
        Assert.That(player.transform.localScale.x, Is.EqualTo(0.575f).Within(0.001f));
    }

    [Test]
    public void FrenzyCapsRefreshesExpiresAndCombinesWithMomentum()
    {
        Acquire("kill_frenzy", 2);
        Acquire("momentum");
        for (int i = 0; i < 9; i++) Kill();
        Assert.That(Value(player, "KillFrenzyCharges"), Is.EqualTo(5));
        Assert.That(Number("MoveSpeedBonus"), Is.EqualTo(0.2f).Within(0.001f));
        Assert.That(Number("WeaponCooldownMultiplier"), Is.EqualTo(1f / 1.4f).Within(0.001f));
        Assert.That(Number("DamageMultiplier"), Is.EqualTo(1.1f).Within(0.001f));
        Tick(3f);
        Kill();
        Tick(3f);
        Assert.That(Value(player, "KillFrenzyCharges"), Is.EqualTo(5), "满层击杀也应刷新持续时间。");
        Tick(0f);
        Assert.That(Number("KillFrenzyTimeRemaining"), Is.EqualTo(1f).Within(0.001f));
        Tick(1f);
        Assert.That(Value(player, "KillFrenzyCharges"), Is.EqualTo(0));
        Assert.That(Number("DamageMultiplier"), Is.EqualTo(1f));
        Assert.That(Number("WeaponCooldownMultiplier"), Is.EqualTo(1f));
    }

    [Test]
    public void OverhealRequiresHealingTalentAndOnlyConvertsOverflow()
    {
        Assert.That(Call(player, "HasPrerequisites", talents["overheal_barrier"]), Is.False);
        Acquire("heal_on_kill");
        Assert.That(Call(player, "HasPrerequisites", talents["overheal_barrier"]), Is.True);
        Acquire("overheal_barrier", 2);
        Call(player, "TakeDamage", 20);
        Call(player, "Heal", 10f);
        Assert.That(Number("CurrentShield"), Is.Zero);
        Call(player, "Heal", 15f);
        Assert.That(Number("CurrentHealth"), Is.EqualTo(100f));
        Assert.That(Number("CurrentShield"), Is.EqualTo(5f));
        Kill();
        Assert.That(Number("CurrentShield"), Is.EqualTo(8f), "自噬在满血时也应转化护盾。");
        Call(player, "Heal", 100f);
        Assert.That(Number("CurrentShield"), Is.EqualTo(30f).Within(0.001f));
        Call(player, "AddShield", 50f);
        Call(player, "Heal", 10f);
        Assert.That(Number("CurrentShield"), Is.EqualTo(80f).Within(0.001f), "不能削减其他来源提供的护盾。");
    }

    [Test]
    public void DashPulseHitsEachEnemyOnceAndHonorsRangeLayerSceneAndCooldown()
    {
        Acquire("dash_pulse", 2);
        player.GetType().GetField("enemyLayers", Private).SetValue(player, (LayerMask)(1 << 7));
        Component near = EnemyAt(new Vector2(2f, 0f));
        near.gameObject.AddComponent<BoxCollider2D>();
        near.gameObject.AddComponent<CircleCollider2D>();
        Component far = EnemyAt(new Vector2(4f, 0f));
        Component excluded = EnemyAt(Vector2.one, 0);
        Component elsewhere = EnemyAt(Vector2.zero);
        otherScene = SceneManager.CreateScene("Other Talent Scene " + Guid.NewGuid(),
            new CreateSceneParameters(LocalPhysicsMode.Physics2D));
        SceneManager.MoveGameObjectToScene(elsewhere.gameObject, otherScene);
        float health = (float)Value(near, "CurrentHealth");
        Call(player, "OnDashPerformed");
        Assert.That(Value(near, "CurrentHealth"), Is.EqualTo(health - 20f));
        Assert.That(Value(near, "IsStunned"), Is.True);
        foreach (var enemy in new[] { far, excluded, elsewhere })
            Assert.That(Value(enemy, "CurrentHealth"), Is.EqualTo(health));
        Call(player, "OnDashPerformed");
        Assert.That(Value(near, "CurrentHealth"), Is.EqualTo(health - 20f));
        Tick(2f);
        Call(player, "OnDashPerformed");
        Assert.That(Value(near, "CurrentHealth"), Is.EqualTo(health - 40f));
    }

    [Test]
    public void RetaliationFiresInitializedRadialBulletsOnlyOnHealthLoss()
    {
        Acquire("retaliation_burst", 2);
        Acquire("double_edge_damage");
        Call(player, "AddShield", 10f);
        Call(player, "TakeDamage", 5);
        Assert.That(Bullets(), Is.Empty);
        EndInvincibility();
        Call(player, "TakeDamage", 10);
        var bullets = Bullets();
        Assert.That(bullets.Length, Is.EqualTo(8));
        Vector2 sum = Vector2.zero;
        foreach (Component bullet in bullets)
        {
            Assert.That(bullet.GetType().GetField("damage").GetValue(bullet), Is.EqualTo(36f));
            Assert.That((float)bullet.GetType().GetField("lifeTimer", Private).GetValue(bullet), Is.GreaterThan(0f));
            Vector2 direction = (Vector2)bullet.GetType().GetField("direction", Private).GetValue(bullet);
            Assert.That(direction.magnitude, Is.EqualTo(1f).Within(0.001f));
            sum += direction;
        }
        Assert.That(sum.magnitude, Is.LessThan(0.001f));
        Call(player, "TakeDamage", 10); // 无敌帧
        EndInvincibility();
        Call(player, "TakeDamage", 10); // 反击冷却
        Assert.That(Bullets().Length, Is.EqualTo(8));
        Tick(4f);
        EndInvincibility();
        Call(player, "TakeDamage", 10);
        Assert.That(Bullets().Length, Is.EqualTo(16));
    }

    [Test]
    public void DeathCannotTriggerRetaliationHealingOrFrenzy()
    {
        Acquire("retaliation_burst");
        Acquire("overheal_barrier");
        Acquire("kill_frenzy");
        Call(player, "TakeDamage", 1000);
        Call(player, "Heal", 1000f);
        Kill();
        Assert.That(Bullets(), Is.Empty);
        Assert.That(Number("CurrentHealth"), Is.Zero);
        Assert.That(Number("CurrentShield"), Is.Zero);
        Assert.That(Value(player, "KillFrenzyCharges"), Is.EqualTo(0));
    }

    [Test]
    public void ReinitializationClearsAllNewTalentEffects()
    {
        Acquire("kill_frenzy");
        Acquire("overheal_barrier");
        Acquire("dash_pulse");
        Acquire("retaliation_burst");
        Kill();
        Call(player, "Initialize", data);
        Kill();
        Call(player, "Heal", 10f);
        Component enemy = EnemyAt(Vector2.one);
        float health = (float)Value(enemy, "CurrentHealth");
        Call(player, "OnDashPerformed");
        Call(player, "TakeDamage", 10);
        Assert.That(Value(player, "KillFrenzyCharges"), Is.EqualTo(0));
        Assert.That(Number("CurrentShield"), Is.Zero);
        Assert.That(Value(enemy, "CurrentHealth"), Is.EqualTo(health));
        Assert.That(Bullets(), Is.Empty);
    }
}
