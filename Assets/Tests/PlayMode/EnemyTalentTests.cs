using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class EnemyTalentTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private Scene scene, previousScene;
    private Component manager, player;
    private object ui, previousUI, previousManager, previousPlayer;
    private FieldInfo managerInstance, uiInstance, activePlayer;
    private float previousTimeScale;
    private IList pool;

    private static Type GameType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Call(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method).Invoke(target, args);
    private static object Value(object target, string property) => target.GetType().GetProperty(property).GetValue(target);
    private static float Number(object target, string property) => (float)Value(target, property);
    private object Runtime => Value(manager, "EnemyTalents");
    private object Talent(string id) => pool.Cast<object>().Single(t => (string)t.GetType().GetField("id").GetValue(t) == id);
    private object Panel(string method, string type) => ui.GetType().GetMethod(method).MakeGenericMethod(GameType(type)).Invoke(ui, null);
    private void Advance(float seconds) => manager.GetType().GetMethod("UpdateEnemyTalents", Private).Invoke(manager, new object[] { seconds });

    [SetUp]
    public void SetUp()
    {
        previousTimeScale = Time.timeScale;
        Time.timeScale = 1f;
        previousScene = SceneManager.GetActiveScene();
        scene = SceneManager.CreateScene("Enemy Talents " + Guid.NewGuid(), new CreateSceneParameters(LocalPhysicsMode.Physics2D));
        SceneManager.SetActiveScene(scene);
        managerInstance = GameType("GameManager").GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
        activePlayer = GameType("Player").GetField("activePlayer", BindingFlags.Static | BindingFlags.NonPublic);
        uiInstance = GameType("UIManager").GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
        previousManager = managerInstance.GetValue(null);
        previousPlayer = activePlayer.GetValue(null);
        previousUI = uiInstance.GetValue(null);
        managerInstance.SetValue(null, null);
        activePlayer.SetValue(null, null);
        ui = Activator.CreateInstance(GameType("UIManager"), true);
        uiInstance.SetValue(null, ui);
        manager = new GameObject("Enemy Talent Test Manager").AddComponent(GameType("GameManager"));
        ((Behaviour)manager).enabled = false; // 不执行自动创建角色的 Start。
        player = new GameObject("Enemy Talent Test Player").AddComponent(GameType("Player"));
        object data = Activator.CreateInstance(GameType("PlayerData"));
        data.GetType().GetField("BaseExperienceToNextLevel").SetValue(data, 100000);
        Call(player, "Initialize", data);
        manager.GetType().GetProperty("Player").SetValue(manager, player);
        object gameData = GameType("GameDataMgr").GetProperty("Instance").GetValue(null);
        pool = (IList)gameData.GetType().GetField("enemyTalentDatas").GetValue(gameData);
        Assert.That(pool.Count, Is.EqualTo(4));
    }

    [TearDown]
    public void TearDown()
    {
        if (manager != null) Object.DestroyImmediate(manager.gameObject);
        Transform canvas = (Transform)ui.GetType().GetField("canvasTrans", Private).GetValue(ui);
        if (canvas != null) Object.DestroyImmediate(canvas.gameObject);
        foreach (GameObject root in scene.GetRootGameObjects()) Object.DestroyImmediate(root);
        uiInstance.SetValue(null, previousUI);
        managerInstance.SetValue(null, previousManager);
        activePlayer.SetValue(null, previousPlayer);
        Time.timeScale = previousTimeScale;
        SceneManager.SetActiveScene(previousScene);
    }

    [UnityTearDown]
    public IEnumerator UnloadScene()
    {
        if (scene.IsValid()) yield return SceneManager.UnloadSceneAsync(scene);
    }

    private IList SingleChoice(object talent)
    {
        var choices = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(GameType("EnemyTalentData")));
        choices.Add(talent);
        return choices;
    }

    private void Apply(string id)
    {
        Call(Runtime, "Tick", Number(Runtime, "NextChoiceTime") - Number(Runtime, "ElapsedTime"));
        object talent = Talent(id);
        Call(Runtime, "RollChoices", SingleChoice(talent), 3);
        Assert.That(Call(Runtime, "Choose", talent), Is.True);
        foreach (Component enemy in Object.FindObjectsByType(GameType("Enemy"), FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (enemy.gameObject.scene == scene) Call(enemy, "RefreshTalentHealth");
    }

    private Component EnemyAt(float waveMultiplier = 1f, string type = "Enemy")
    {
        var go = new GameObject("Enemy Talent Target");
        go.AddComponent<Rigidbody2D>();
        Component enemy = go.AddComponent(GameType(type));
        object data = Activator.CreateInstance(GameType("EnemyData"));
        data.GetType().GetField("MaxHealth").SetValue(data, 100f);
        data.GetType().GetField("MoveSpeed").SetValue(data, 2f);
        enemy.GetType().GetMethod("Initialize", new[] { data.GetType(), typeof(float) }).Invoke(enemy, new[] { data, (object)waveMultiplier });
        return enemy;
    }

    [Test]
    public void TimerOffersUniqueChoicesEveryFiveMinutesAndRejectsDoubleSelection()
    {
        Call(Runtime, "Tick", 299f);
        Assert.That(((IList)Call(Runtime, "RollChoices", pool, 3)).Count, Is.Zero);
        Call(Runtime, "Tick", 1f);
        var choices = (IList)Call(Runtime, "RollChoices", pool, 3);
        Assert.That(choices.Count, Is.EqualTo(3));
        Assert.That(choices.Cast<object>().Distinct().Count(), Is.EqualTo(3));
        Call(Runtime, "Tick", 900f);
        Assert.That(Number(Runtime, "ElapsedTime"), Is.EqualTo(300f));
        Assert.That(Call(Runtime, "Choose", Activator.CreateInstance(GameType("EnemyTalentData"))), Is.False);
        Assert.That(Call(Runtime, "Choose", choices[0]), Is.True);
        Assert.That(Call(Runtime, "Choose", choices[0]), Is.False);
        Assert.That(Value(Runtime, "UpgradeCount"), Is.EqualTo(1));
        Assert.That(Number(Runtime, "NextChoiceTime"), Is.EqualTo(600f));
        Call(Runtime, "Tick", 299f);
        Assert.That(Value(Runtime, "IsChoiceDue"), Is.False);
        Call(Runtime, "Tick", 1f);
        Assert.That(Value(Runtime, "IsChoiceDue"), Is.True);
    }

    [UnityTest]
    public IEnumerator RealPanelPausesShowsThreeChoicesAndResumesAfterOneClick()
    {
        Advance(299f);
        Assert.That(Panel("GetPanel", "EnemyTalentPanel"), Is.Null);
        Time.timeScale = 0f;
        Advance(60f);
        Assert.That(Number(Runtime, "ElapsedTime"), Is.EqualTo(299f));
        Time.timeScale = 1f;
        Advance(1f);
        var panel = (Component)Panel("GetPanel", "EnemyTalentPanel");
        Assert.That(panel, Is.Not.Null);
        Assert.That(Time.timeScale, Is.Zero);
        var buttons = (Button[])panel.GetType().GetField("buttons").GetValue(panel);
        var titles = (Text[])panel.GetType().GetField("texts").GetValue(panel);
        var descriptions = (Text[])panel.GetType().GetField("intrTexts").GetValue(panel);
        Assert.That(buttons.Length, Is.EqualTo(3));
        Assert.That(titles.All(t => !string.IsNullOrWhiteSpace(t.text)), Is.True);
        Assert.That(descriptions.All(t => !string.IsNullOrWhiteSpace(t.text)), Is.True);
        buttons[0].onClick.Invoke();
        buttons[1].onClick.Invoke();
        Assert.That(Value(Runtime, "UpgradeCount"), Is.EqualTo(1));
        yield return null; // UIManager 的 Destroy 在帧末释放暂停。
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Advance(299f);
        Assert.That(Panel("GetPanel", "EnemyTalentPanel"), Is.Null);
        Advance(1f);
        Assert.That(Panel("GetPanel", "EnemyTalentPanel"), Is.Not.Null);
    }

    [Test]
    public void PlayerTalentPanelDefersEnemyChoiceAndOverlappingPanelsKeepPause()
    {
        Time.timeScale = 0.5f;
        var playerPanel = (Component)Panel("ShowPanel", "TalentPanel");
        Advance(300f);
        Assert.That(Number(Runtime, "ElapsedTime"), Is.Zero);
        var enemyPanel = (Component)Panel("ShowPanel", "EnemyTalentPanel");
        playerPanel.gameObject.SetActive(false);
        Assert.That(Time.timeScale, Is.Zero);
        enemyPanel.gameObject.SetActive(false);
        Assert.That(Time.timeScale, Is.EqualTo(0.5f));
    }

    [Test]
    public void GameOverStopsTimerAndClosesPendingPanel()
    {
        Advance(300f);
        Assert.That(Value(Runtime, "IsChoosing"), Is.True);
        Call(manager, "OnPlayerDeath");
        Assert.That(Value(Runtime, "IsChoosing"), Is.False);
        Assert.That(Panel("GetPanel", "EnemyTalentPanel"), Is.Null);
        Component overPanel = (Component)Panel("GetPanel", "OverPanel");
        Assert.That(overPanel, Is.Not.Null);
        Assert.That(((Text)overPanel.GetType().GetField("txtOver").GetValue(overPanel)).text, Is.EqualTo("失败"));
        Assert.That(Time.timeScale, Is.Zero);
        Advance(600f);
        Assert.That(Number(Runtime, "ElapsedTime"), Is.EqualTo(300f));
    }

    [Test]
    public void HealthStacksWithSpawnerGrowthPreservesPercentageAndDoesNotMutateData()
    {
        Component enemy = EnemyAt(2f);
        Call(enemy, "TakeDamage", 50);
        Apply("enemy_giant");
        Assert.That(Number(enemy, "MaxHealth"), Is.EqualTo(600f));
        Assert.That(Number(enemy, "CurrentHealth"), Is.EqualTo(450f));
        Call(enemy, "RefreshTalentHealth");
        Assert.That(Number(enemy, "CurrentHealth"), Is.EqualTo(450f));
        Apply("enemy_giant");
        Assert.That(Number(enemy, "MaxHealth"), Is.EqualTo(1800f));
        Assert.That(Number(enemy, "CurrentHealth"), Is.EqualTo(1350f));
        object data = Value(enemy, "Data");
        Assert.That(data.GetType().GetField("MaxHealth").GetValue(data), Is.EqualTo(100f));
        Component newlySpawned = EnemyAt(2f);
        Assert.That(Number(newlySpawned, "CurrentHealth"), Is.EqualTo(1800f));
        Component summoned = EnemyAt();
        Assert.That(Number(summoned, "CurrentHealth"), Is.EqualTo(900f));
    }

    [Test]
    public void ArmorKeepsFractionalDamageAndStacksMultiplicatively()
    {
        Component enemy = EnemyAt();
        Apply("enemy_armor");
        Call(enemy, "TakeDamage", 1);
        Assert.That(Number(enemy, "CurrentHealth"), Is.EqualTo(99.5f));
        Apply("enemy_armor");
        Call(enemy, "TakeDamage", 1);
        Assert.That(Number(enemy, "CurrentHealth"), Is.EqualTo(99.25f));
    }

    [TestCase("Enemy")]
    [TestCase("EnemyBullet")]
    [TestCase("TrapDamage")]
    [TestCase("Shockwave")]
    public void DamageUpgradeAffectsEveryEnemyDamageSourceExactlyOnce(string type)
    {
        Apply("enemy_ferocity");
        Apply("enemy_ferocity");
        Collider2D collider = player.gameObject.AddComponent<CircleCollider2D>();
        Component source = new GameObject("Damage Source").AddComponent(GameType(type));
        if (type == "EnemyBullet")
        {
            source.GetType().GetField("hitLayers", Private).SetValue(source, (LayerMask)(~0));
            Call(source, "Initialize", Vector2.right, 10);
        }
        if (type == "TrapDamage" || type == "Shockwave")
            source.GetType().GetField("damage", Private).SetValue(source, 10);
        string hit = type == "Enemy" ? "TryContactDamage" : "OnTriggerEnter2D";
        source.GetType().GetMethod(hit, Private).Invoke(source, new object[] { collider });
        Assert.That(Number(player, "CurrentHealth"), Is.EqualTo(60f));
    }

    [TestCase("Enemy", 3.5f)]
    [TestCase("ChaserEnemy", 7f)]
    [TestCase("BerserkerEnemy", 7f)]
    public void HasteAffectsBaseAndCustomEnemyMovement(string type, float expectedSpeed)
    {
        Component enemy = EnemyAt(1f, type);
        player.transform.position = new Vector3(10f, 0f);
        Apply("enemy_haste");
        if (type != "Enemy") enemy.GetType().GetField("playerTarget", Private).SetValue(enemy, player.transform);
        string method = type == "Enemy" ? "MoveTowardsPlayer" : type == "ChaserEnemy" ? "FixedUpdate" : "UpdateMovement";
        enemy.GetType().GetMethod(method, Private).Invoke(enemy, null);
        Assert.That(enemy.GetComponent<Rigidbody2D>().linearVelocity.x, Is.EqualTo(expectedSpeed).Within(0.001f));
        if (type == "BerserkerEnemy")
        {
            enemy.GetType().GetMethod("ApplyChargeForce", Private).Invoke(enemy, null);
            Assert.That(enemy.GetComponent<Rigidbody2D>().linearVelocity.x, Is.EqualTo(17.5f).Within(0.001f));
        }
    }

    [Test]
    public void NewRunResetsAllEnemyMultipliersAndSchedule()
    {
        foreach (string id in new[] { "enemy_giant", "enemy_ferocity", "enemy_haste", "enemy_armor" }) Apply(id);
        Object.DestroyImmediate(manager.gameObject);
        manager = new GameObject("New Run Manager").AddComponent(GameType("GameManager"));
        ((Behaviour)manager).enabled = false;
        Assert.That(Number(Runtime, "ElapsedTime"), Is.Zero);
        Assert.That(Number(Runtime, "NextChoiceTime"), Is.EqualTo(300f));
        foreach (string stat in new[] { "HealthMultiplier", "DamageMultiplier", "MoveSpeedMultiplier", "DamageTakenMultiplier" })
            Assert.That(Number(Runtime, stat), Is.EqualTo(1f));
        Assert.That(Value(Runtime, "UpgradeCount"), Is.Zero);
        Assert.That(Number(EnemyAt(), "CurrentHealth"), Is.EqualTo(100f));
    }

    [Test]
    public void ShutdownHandlesPanelDestroyedBeforeManagerAndAllowsReopening()
    {
        Advance(300f);
        var panel = (Component)Panel("GetPanel", "EnemyTalentPanel");
        Object.DestroyImmediate(panel.gameObject);
        Assert.DoesNotThrow(() => Object.DestroyImmediate(manager.gameObject));
        Assert.That(Panel("GetPanel", "EnemyTalentPanel"), Is.Null);
        var reopened = (Component)Panel("ShowPanel", "EnemyTalentPanel");
        Assert.That(reopened != null, Is.True);
        Assert.That(Time.timeScale, Is.Zero);
        Object.DestroyImmediate(reopened.gameObject);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
    }

    [UnityTest]
    public IEnumerator TwentyMinutesWinsInsteadOfOpeningFourthEnemyUpgrade()
    {
        var controller = (Behaviour)player.gameObject.AddComponent(GameType("PlayerController"));
        player.GetType().GetField("controller", Private).SetValue(player, controller);
        for (int i = 0; i < 3; i++)
        {
            Advance(300f);
            var panel = (Component)Panel("GetPanel", "EnemyTalentPanel");
            Assert.That(panel, Is.Not.Null);
            ((Button[])panel.GetType().GetField("buttons").GetValue(panel))[0].onClick.Invoke();
            yield return null;
        }

        Advance(299f);
        Assert.That(Value(manager, "IsVictory"), Is.False);
        Time.timeScale = 0f;
        Advance(100f);
        Assert.That(Number(Runtime, "ElapsedTime"), Is.EqualTo(1199f));
        Time.timeScale = 1f;
        Advance(1f);
        Assert.That(Value(manager, "IsVictory"), Is.True);
        Assert.That(Value(manager, "IsGameOver"), Is.True);
        Assert.That(Time.timeScale, Is.Zero);
        Component overPanel = (Component)Panel("GetPanel", "OverPanel");
        Assert.That(overPanel, Is.Not.Null);
        Assert.That(((Text)overPanel.GetType().GetField("txtOver").GetValue(overPanel)).text, Is.EqualTo("胜利"));
        Assert.That(controller.enabled, Is.False);
        Assert.That(Value(Runtime, "UpgradeCount"), Is.EqualTo(3));
        Assert.That(Panel("GetPanel", "EnemyTalentPanel"), Is.Null);
        Assert.That(Value(Runtime, "IsChoosing"), Is.False);

        float health = Number(player, "CurrentHealth");
        int experience = (int)Value(player, "Experience");
        Call(player, "TakeDamage", 10000);
        GameType("Player").GetMethod("NotifyEnemyDefeated", new[] { typeof(int) }).Invoke(null, new object[] { 100 });
        Call(manager, "OnPlayerDeath");
        Advance(100f);
        Assert.That(Number(player, "CurrentHealth"), Is.EqualTo(health));
        Assert.That(Value(player, "Experience"), Is.EqualTo(experience));
        Assert.That(Number(Runtime, "ElapsedTime"), Is.EqualTo(1200f));
        Assert.That(Value(manager, "IsVictory"), Is.True);
    }

    [Test]
    public void DefeatCannotBecomeVictoryAndNewRunReleasesVictoryPause()
    {
        Call(manager, "OnPlayerDeath");
        Advance(1200f);
        Assert.That(Value(manager, "IsVictory"), Is.False);
        Object.DestroyImmediate(manager.gameObject);
        manager = new GameObject("Victory Test Manager").AddComponent(GameType("GameManager"));
        ((Behaviour)manager).enabled = false;
        manager.GetType().GetProperty("Player").SetValue(manager, player);
        Advance(1200f);
        Assert.That(Value(manager, "IsVictory"), Is.True);
        Assert.That(Time.timeScale, Is.Zero);
        Object.DestroyImmediate(manager.gameObject);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        manager = new GameObject("Next Run Manager").AddComponent(GameType("GameManager"));
        ((Behaviour)manager).enabled = false;
        Assert.That(Value(manager, "IsVictory"), Is.False);
        Assert.That(Value(manager, "IsGameOver"), Is.False);
        Assert.That(Number(Runtime, "ElapsedTime"), Is.Zero);
    }
}
